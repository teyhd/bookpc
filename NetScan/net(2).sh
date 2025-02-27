#!/bin/bash

IFS=$'\n\t'

# ========== Конфигурация ==========
WORK_DIR="/var/www/html/NetScan"
DB_LOCAL="/var/lib/bind/local.hosts"
MY_CNF="$WORK_DIR/.my.cnf"

LOG_DIR="$WORK_DIR/logs"
DAILY_LOG="$LOG_DIR/$(date +'%Y-%m-%d').log"
CURRENT_LOG="$WORK_DIR/current.log"
LOCK_FILE="$WORK_DIR/network_scanner.lock"
# ========== Конец конфигурации ==========

# Создание необходимых директорий
mkdir -p "$WORK_DIR" "$LOG_DIR"

# Очистка current.log от записей старше 24 часов
prune_current_log() {
    local cutoff
    cutoff=$(date -d "24 hours ago" "+%Y-%m-%d %H:%M:%S")
    if [[ -f "$CURRENT_LOG" ]]; then
        awk -v cutoff="$cutoff" '{
            if ($0 ~ /^[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2}/) {
                logtime = substr($0,1,19);
                if (logtime >= cutoff) print $0;
            } else {
                print $0;
            }
        }' "$CURRENT_LOG" > "${CURRENT_LOG}.tmp" && mv "${CURRENT_LOG}.tmp" "$CURRENT_LOG"
    else
        touch "$CURRENT_LOG"
    fi
}
prune_current_log

# Функция логирования с временной меткой
log_msg() {
    local level="$1" message="$2"
    local timestamp
    timestamp=$(date "+%Y-%m-%d %H:%M:%S")
    echo "$timestamp [$level] - $message" | tee -a "$DAILY_LOG" "$CURRENT_LOG"
}

# Предотвращение одновременного запуска скрипта
if [[ -f "$LOCK_FILE" ]]; then
    log_msg "WARNING" "Скрипт уже запущен. Выход."
    exit 1
fi
touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"; log_msg "INFO" "Lock file удален."' EXIT

# Проверка наличия файла конфигурации MySQL
if [[ ! -f "$MY_CNF" ]]; then
    log_msg "ERROR" "Файл конфигурации MySQL ($MY_CNF) не найден. Выход."
    exit 1
fi

# Проверка необходимых утилит
for util in nmap avahi-browse nmblookup dig mysql timeout; do
    if ! command -v "$util" &>/dev/null; then
        log_msg "ERROR" "Утилита $util не установлена. Выход."
        exit 1
    fi
done

# Функция выполнения SQL-запросов
execute_sql() {
    local query="$1"
    mysql --defaults-extra-file="$MY_CNF" -sN -e "$query"
}

# Функция очистки имени устройства
clean_name() {
    local input="$1"
    echo "$input" | tr -cd '[:alnum:]-' | sed 's/^-*//;s/-*$//' | tr '[:upper:]' '[:lower:]'
}

# Функция генерации более понятных имен
generate_device_name() {
    local mac="$1" vendor="$2" ip="$3"
    if [[ -n "$vendor" ]]; then
        name=$(echo "$vendor" | awk '{print tolower($1)}' | tr -cd '[:alnum:]')
    else
        name="device"
    fi
    short_ip=$(echo "$ip" | awk -F'.' '{print $3"-"$4}')
    echo "${name}-${short_ip}"
}

# Функция актуализации локального DNS-файла
update_dns_file() {
    log_msg "INFO" "Обновление локального DNS файла: $DB_LOCAL"

    {
        echo "\$TTL 3600"
        echo "@ IN SOA ns.local. root.local. ("
        echo "  1 ; Serial"
        echo "  3600 ; Refresh"
        echo "  86400 ; Retry"
        echo "  2419200 ; Expire"
        echo "  3600 ) ; Negative Cache TTL"
        echo ""
        echo "@ IN NS ns.local."
        echo "ns IN A 10.10.88.1"
        echo "net IN A 172.24.0.226"
        echo "dock IN A 172.24.0.228"
        echo "db IN A 172.24.0.227"
        echo "fs IN A 172.24.0.230"
        echo "cams IN A 172.24.0.231"
        echo "rasp IN A 172.24.0.203"
        echo "oldsrv IN A 172.24.0.229"
        echo "bux IN A 172.24.0.201"
        # Добавляем устройства из БД в DNS, если их IP отсутствуют в статическом списке
        execute_sql "SELECT name, ip FROM devices WHERE connected = TRUE;" | while read -r name ip; do
            if ! grep -q " $ip\$" "$DB_LOCAL"; then
                echo "$name IN A $ip"
            fi
        done
    } > "$DB_LOCAL"

    # Перезапуск службы bind9
    systemctl restart bind9
    log_msg "INFO" "Файл $DB_LOCAL обновлен и bind9 перезапущен."
}

# Основная обработка устройств
process_device() {
    local ip="$1" mac="$2" vendor="$3"
    local avahi_name netbios_name rdns_name name device_id db_ip db_mac db_name

    # Поиск имени через avahi, NetBIOS и rDNS
    avahi_name=$(awk -F';' -v ip="$ip" '$8==ip {gsub(/\..*$/, "", $7); print $7; exit}' "$WORK_DIR/avahi_output.txt")
    if [[ -z "$avahi_name" ]]; then
        netbios_name=$(nmblookup -A "$ip" -r 2>/dev/null | awk '/<00>/ && !/<GROUP>/ {print $1; exit}' | tr -d '\n')
    fi
    if [[ -z "$avahi_name" && -z "$netbios_name" ]]; then
        rdns_name=$(dig +short -x "$ip" | sed 's/\.$//')
    fi

    name=$(clean_name "${avahi_name:-${netbios_name:-${rdns_name}}}")

    # Если имя не найдено, генерируем его
    if [[ -z "$name" ]]; then
        name=$(generate_device_name "$mac" "$vendor" "$ip")
        log_msg "INFO" "Не удалось определить имя, используем сгенерированное: $name ($ip)"
    fi

    # Проверка существования устройства в базе
    read -r device_id db_ip db_mac db_name < <(execute_sql "
        SELECT id, ip, mac, name FROM devices 
        WHERE (mac = '$mac' AND mac IS NOT NULL) 
           OR (ip = '$ip' AND ip IS NOT NULL) 
           OR (name = '$name' AND name IS NOT NULL) 
        LIMIT 1;
    ")

    if [[ -n "$device_id" ]]; then
        # Обновление записи устройства
        execute_sql "
            UPDATE devices 
            SET name = '$name', ip = '$ip', mac = '$mac', vendor = '$vendor', last_seen = NOW(), connected = TRUE 
            WHERE id = $device_id;
        "
        log_msg "INFO" "Обновлено устройство: $name ($ip)"
    else
        # Добавление нового устройства
        execute_sql "
            INSERT INTO devices (name, ip, mac, vendor, connected, last_seen) 
            VALUES ('$name', '$ip', '$mac', '$vendor', TRUE, NOW());
        "
        log_msg "INFO" "Добавлено новое устройство: $name ($ip)"
    fi
}

# Обработка устройств из файла (без многопоточности)
while IFS='|' read -r ip mac vendor; do
    process_device "$ip" "$mac" "$vendor"
done < "$WORK_DIR/network_scan.txt"

# Актуализация локального DNS-файла после обработки всех устройств
update_dns_file

log_msg "INFO" "Сканирование завершено."
