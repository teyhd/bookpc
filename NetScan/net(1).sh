#!/bin/bash

IFS=$'\n\t'

# ========== Конфигурация ==========
NETWORK="172.24.0.0/24"
TIMEOUT_INTERVAL="15 MINUTE"
MAX_JOBS=10  # Максимальное количество параллельных процессов

WORK_DIR="/var/www/html/NetScan"
LOCK_FILE="$WORK_DIR/network_scanner.lock"
MY_CNF="$WORK_DIR/.my.cnf"

LOG_DIR="$WORK_DIR/logs"
DAILY_LOG="$LOG_DIR/$(date +'%Y-%m-%d').log"
CURRENT_LOG="$WORK_DIR/current.log"
# ========== Конец конфигурации ==========

# Создание необходимых директорий
mkdir -p "$WORK_DIR" "$LOG_DIR"

# Функция для очистки current.log от записей старше 24 часов
prune_current_log() {
    local cutoff
    cutoff=$(date -d "24 hours ago" "+%Y-%m-%d %H:%M:%S")
    if [[ -f "$CURRENT_LOG" ]]; then
        awk -v cutoff="$cutoff" '{
            if ($0 ~ /^[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2}/) {
                logtime = substr($0,1,19)
                if (logtime >= cutoff) print $0
            } else {
                print $0
            }
        }' "$CURRENT_LOG" > "${CURRENT_LOG}.tmp" && mv "${CURRENT_LOG}.tmp" "$CURRENT_LOG"
    else
        touch "$CURRENT_LOG"
    fi
}

prune_current_log

# Функция логирования с указанием временной метки и уровня
log_msg() {
    local level="$1"
    local message="$2"
    local timestamp
    timestamp=$(date "+%Y-%m-%d %H:%M:%S")
    echo "$timestamp [$level] - $message" | tee -a "$DAILY_LOG" "$CURRENT_LOG"
}

# Проверка наличия файла конфигурации MySQL
if [[ ! -f "$MY_CNF" ]]; then
    log_msg "ERROR" "Файл конфигурации MySQL ($MY_CNF) не найден. Выход."
    exit 1
fi

# Проверка блокировки – предотвращение одновременного запуска скрипта
if [[ -f "$LOCK_FILE" ]]; then
    log_msg "WARNING" "Скрипт уже запущен. Выход."
    exit 1
fi
touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"; log_msg "INFO" "Lock file удален."' EXIT

# Проверка необходимых утилит
for util in nmap avahi-browse nmblookup dig mysql; do
    if ! command -v "$util" &>/dev/null; then
        log_msg "ERROR" "Утилита $util не установлена. Выход."
        exit 1
    fi
done

# Функция для выполнения SQL-запросов (без заголовков)
execute_sql() {
    local query="$1"
    mysql --defaults-extra-file="$MY_CNF" -sN -e "$query"
}

# Функция для очистки имени устройства
clean_name() {
    local input="$1"
    echo "$input" | tr -cd '[:alnum:]-' | sed 's/^[-]*//;s/[-]*$//' | tr '[:upper:]' '[:lower:]'
}

# Функция для проверки шаблонного имени (например, device-IP)
is_device_ip_name() {
    [[ "$1" =~ ^device-([0-9]{1,3}-){3}[0-9]{1,3}$ ]]
}

log_msg "INFO" "Начало сканирования сети $NETWORK..."

# Параллельный запуск nmap и avahi-browse
nmap -sn -n -T4 "$NETWORK" > "$WORK_DIR/network_scan_raw.txt" &
nmap_pid=$!
avahi-browse -r -a -p 2>/dev/null > "$WORK_DIR/avahi_output.txt" &
avahi_pid=$!
wait $nmap_pid
log_msg "INFO" "Сканирование nmap завершено."
wait $avahi_pid
log_msg "INFO" "Сбор данных avahi-browse завершен."

# Обработка вывода nmap для извлечения IP, MAC и vendor
awk '
    /^Nmap scan report for/ {
        if(ip != "") { print ip "|" mac "|" vendor; }
        ip = $NF; gsub(/[()]/, "", ip);
    }
    /MAC Address:/ {
        mac = $3;
        vendor = "";
        if(match($0, /\(.*\)/)) {
            vendor = substr($0, RSTART+1, RLENGTH-2);
        }
    }
    END { if(ip != "") print ip "|" mac "|" vendor; }
' "$WORK_DIR/network_scan_raw.txt" > "$WORK_DIR/network_scan.txt"
log_msg "INFO" "Обработка вывода nmap завершена."

# Функция для обработки одного устройства
process_device() {
    local ip="$1" mac="$2" vendor="$3"
    local avahi_name netbios_name rdns_name name

    # Получаем имя устройства из avahi-browse
    avahi_name=$(awk -F';' -v ip="$ip" '$8==ip {gsub(/\..*$/, "", $7); print $7; exit}' "$WORK_DIR/avahi_output.txt")
    
    if [[ -z "$avahi_name" ]]; then
        netbios_name=$(nmblookup -A "$ip" -r 2>/dev/null | awk '/<00>/ && !/<GROUP>/ {print $1; exit}' | tr -d '\n')
    else
        netbios_name=""
    fi
    
    if [[ -z "${avahi_name}${netbios_name}" ]]; then
        rdns_name=$(dig +short -x "$ip" | sed 's/\.$//')
    else
        rdns_name=""
    fi

    name=$(clean_name "${avahi_name:-${netbios_name:-${rdns_name}}}")

    # Если имя соответствует шаблонному, пропускаем устройство
    if is_device_ip_name "$name"; then
        log_msg "INFO" "Пропущено устройство с шаблонным именем: $name ($ip)"
        return
    fi

    if [[ -n "$name" ]]; then
        device_row=$(execute_sql "SELECT id, connected, ip FROM devices WHERE name = '$name' LIMIT 1;")
        
        if [[ -z "$device_row" ]]; then
            execute_sql "INSERT INTO devices (name, ip, mac, vendor, connected, last_seen) VALUES ('$name', '$ip', '$mac', '$vendor', TRUE, NOW());"
            device_id=$(execute_sql "SELECT LAST_INSERT_ID();")
            execute_sql "INSERT INTO device_history (device_id, event_type) VALUES ($device_id, 'connected');"
            log_msg "INFO" "Добавлено новое устройство: $name ($ip)"
        else
            device_id=$(echo "$device_row" | awk '{print $1}')
            device_connected=$(echo "$device_row" | awk '{print $2}')
            device_old_ip=$(echo "$device_row" | awk '{print $3}')
            
            if [[ "$device_old_ip" != "$ip" ]]; then
                execute_sql "UPDATE devices SET ip = '$ip', mac = '$mac', vendor = '$vendor', last_seen = NOW(), connected = TRUE WHERE id = $device_id;"
                execute_sql "INSERT INTO device_history (device_id, event_type, old_ip, new_ip) VALUES ($device_id, 'ip_changed', '$device_old_ip', '$ip');"
                log_msg "INFO" "IP устройства $name изменён: $device_old_ip → $ip"
            else
                execute_sql "UPDATE devices SET last_seen = NOW(), mac = '$mac', vendor = '$vendor', connected = TRUE WHERE id = $device_id;"
                if [[ "$device_connected" == "0" || "$device_connected" == "FALSE" ]]; then
                    execute_sql "INSERT INTO device_history (device_id, event_type) VALUES ($device_id, 'connected');"
                    log_msg "INFO" "Устройство $name повторно подключилось ($ip)"
                fi
            fi
        fi
    else
        log_msg "WARNING" "Не удалось определить имя для: $ip"
    fi
}

# Функция для контроля количества фоновых задач
wait_for_jobs() {
    while [ "$(jobs -rp | wc -l)" -ge "$MAX_JOBS" ]; do
        sleep 0.1
    done
}

# Обработка устройств из файла в параллельном режиме
while IFS='|' read -r ip mac vendor; do
    wait_for_jobs
    process_device "$ip" "$mac" "$vendor" &
done < "$WORK_DIR/network_scan.txt"

wait
log_msg "INFO" "Обработка всех устройств завершена."

# Обновление статуса устройств, не обнаруженных в течение TIMEOUT_INTERVAL
execute_sql "
    UPDATE devices
    SET connected = FALSE
    WHERE last_seen < NOW() - INTERVAL $TIMEOUT_INTERVAL
      AND connected = TRUE;
"
execute_sql "
    INSERT INTO device_history (device_id, event_type)
    SELECT id, 'disconnected'
    FROM devices
    WHERE last_seen < NOW() - INTERVAL $TIMEOUT_INTERVAL
      AND connected = FALSE
      AND (
          SELECT IFNULL((SELECT event_type FROM device_history WHERE device_id = devices.id ORDER BY event_time DESC LIMIT 1), '') <> 'disconnected'
      );
"
log_msg "INFO" "Обновление статуса устройств завершено."
log_msg "INFO" "Сканирование завершено."
