#!/bin/bash
#set -euo pipefail

# ========== Конфигурация ==========
NETWORK="172.24.0.0/24"
TIMEOUT_INTERVAL="20 MINUTE"

# Директория для рабочих файлов и логов
WORK_DIR="/var/www/html/NetScan"
LOCK_FILE="$WORK_DIR/network_scanner.lock"
LOG_FILE="$WORK_DIR/network_scanner.log"

MY_CNF="$WORK_DIR/.my.cnf"
# ========== Конец конфигурации ==========

# Перенаправляем вывод и ошибки в лог-файл
exec >> "$LOG_FILE" 2>&1

# Проверка блокировки
if [ -f "$LOCK_FILE" ]; then
    echo "$(date) - Скрипт уже запущен. Выход."
    exit 1
fi
trap 'rm -f "$LOCK_FILE"' EXIT
touch "$LOCK_FILE"

# Проверка наличия необходимых утилит
required_utils=("nmap" "avahi-browse" "nmblookup" "dig" "mysql")
for util in "${required_utils[@]}"; do
    if ! command -v "$util" &>/dev/null; then
        echo "$(date) - Утилита $util не установлена."
        exit 1
    fi
done

# Функция для выполнения SQL-запросов через MySQL с использованием .my.cnf
execute_sql() {
    local query="$1"
    mysql --defaults-extra-file="$MY_CNF" -e "$query"
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

echo "$(date) - Сканирование сети..."

# Сканирование сети
# Сохраняем вывод nmap в рабочую директорию
nmap -sn -n "$NETWORK" > "$WORK_DIR/network_scan_raw.txt"

# Кэшируем вывод avahi-browse для повышения производительности
avahi-browse -r -a -t -p 2>/dev/null > "$WORK_DIR/avahi_output.txt"

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

# Обработка каждого обнаруженного устройства
while IFS='|' read -r ip mac vendor; do
    name=""
    # Получаем имя устройства из кэша avahi
    avahi_name=$(awk -F';' -v ip="$ip" '$8==ip {gsub(/\..*$/, "", $7); print $7; exit}' "$WORK_DIR/avahi_output.txt")
    
    if [ -z "$avahi_name" ]; then
        netbios_name=$(nmblookup -A "$ip" 2>/dev/null | awk '/<00>/ && !/<GROUP>/ {print $1; exit}' | tr -d '\n')
    else
        netbios_name=""
    fi
    
    if [ -z "$avahi_name$netbios_name" ]; then
        rdns_name=$(dig +short -x "$ip" | sed 's/\.$//')
    else
        rdns_name=""
    fi
    name=$(clean_name "${avahi_name:-${netbios_name:-${rdns_name}}}")
    
    if is_device_ip_name "$name"; then
        echo "$(date) - Пропущено устройство с шаблонным именем: $name ($ip)"
        continue
    fi
    
    if [ -n "$name" ]; then
        device_row=$(execute_sql "SELECT id, connected, ip FROM devices WHERE name = '$name' LIMIT 1;" | tail -n +2)
        
        if [ -z "$device_row" ]; then
            # Новое устройство – добавляем в таблицу devices и фиксируем событие подключения
            execute_sql "INSERT INTO devices (name, ip, mac, vendor, connected, last_seen) VALUES ('$name', '$ip', '$mac', '$vendor', TRUE, NOW());"
            device_id=$(execute_sql "SELECT LAST_INSERT_ID();" | tail -n +2)
            execute_sql "INSERT INTO device_history (device_id, event_type) VALUES ($device_id, 'connected');"
            echo "$(date) - Добавлено новое устройство: $name ($ip)"
        else
            device_id=$(echo "$device_row" | awk '{print $1}')
            device_connected=$(echo "$device_row" | awk '{print $2}')
            device_old_ip=$(echo "$device_row" | awk '{print $3}')
            
            if [ "$device_old_ip" != "$ip" ]; then
                execute_sql "UPDATE devices SET ip = '$ip', mac = '$mac', vendor = '$vendor', last_seen = NOW(), connected = TRUE WHERE id = $device_id;"
                execute_sql "INSERT INTO device_history (device_id, event_type, old_ip, new_ip) VALUES ($device_id, 'ip_changed', '$device_old_ip', '$ip');"
                echo "$(date) - IP устройства $name изменён: $device_old_ip → $ip"
            else
                execute_sql "UPDATE devices SET last_seen = NOW(), mac = '$mac', vendor = '$vendor', connected = TRUE WHERE id = $device_id;"
                if [[ "$device_connected" == "0" || "$device_connected" == "FALSE" ]]; then
                    execute_sql "INSERT INTO device_history (device_id, event_type) VALUES ($device_id, 'connected');"
                    echo "$(date) - Устройство $name повторно подключилось ($ip)"
                fi
            fi
        fi
    else
        echo "$(date) - Не удалось определить имя для: $ip"
    fi
done < "$WORK_DIR/network_scan.txt"

# Обновляем статус устройств, не обнаруженных в течение TIMEOUT_INTERVAL
execute_sql "
    UPDATE devices
    SET connected = FALSE
    WHERE last_seen < NOW() - INTERVAL $TIMEOUT_INTERVAL
      AND connected = TRUE;
"

# Фиксируем событие отключения, если последнее событие для устройства не было 'disconnected'
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

echo "$(date) - Сканирование завершено."
