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
SUBNET="172.24.0.0/24"
TIMEOUT_INTERVAL="15 MINUTE"
# ========== Конец конфигурации ==========

mkdir -p "$WORK_DIR" "$LOG_DIR"

log_msg() {
    local level="$1" message="$2"
    local timestamp=$(date "+%Y-%m-%d %H:%M:%S")
    echo "$timestamp [$level] - $message" | tee -a "$DAILY_LOG" "$CURRENT_LOG"
}

prune_current_log() {
    local cutoff=$(date -d "24 hours ago" "+%Y-%m-%d %H:%M:%S")
    awk -v cutoff="$cutoff" '$0 ~ /^[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2}/ {logtime = substr($0,1,19); if (logtime >= cutoff) print $0} ! /^[0-9]/ {print $0}' "$CURRENT_LOG" > "$CURRENT_LOG.tmp" && mv "$CURRENT_LOG.tmp" "$CURRENT_LOG"
}
prune_current_log

if [[ -f "$LOCK_FILE" ]]; then
    log_msg "WARNING" "Скрипт уже запущен. Выход."
    exit 1
fi
touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"' EXIT

if [[ ! -f "$MY_CNF" ]]; then
    log_msg "ERROR" "Файл конфигурации MySQL ($MY_CNF) не найден. Выход."
    exit 1
fi

for util in nmap avahi-browse nmblookup dig mysql timeout host arp-scan; do
    if ! command -v "$util" &>/dev/null; then
        log_msg "ERROR" "Утилита $util не установлена. Выход."
        exit 1
    fi
done

execute_sql() {
    local query="$1"
    mysql --defaults-extra-file="$MY_CNF" -sN -e "$query"
}

clean_name() {
    echo "$1" | tr -cd '[:alnum:]-' | tr '[:upper:]' '[:lower:]'
}

generate_device_name() {
    local mac="$1" vendor="$2" ip="$3"
    local short_ip=$(echo "$ip" | awk -F'.' '{print $3"-"$4}')
    echo "${vendor:-device}-$short_ip"
}

update_dns_file() {
    log_msg "INFO" "Обновление локального DNS файла: $DB_LOCAL"

    serial=$(date +%Y%m%d%H)  # Генерируем уникальный Serial (YYYYMMDDHH)

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

        # Генерация A-записей из базы данных
        execute_sql "SELECT name, ip FROM devices WHERE connected = TRUE;" | while IFS=$'\t' read -r name ip; do
            [[ -n "$name" && -n "$ip" ]] && echo "$name IN A $ip"
        done
    } > "$DB_LOCAL"

    systemctl restart bind9
    log_msg "INFO" "DNS файл обновлен, Serial: $serial, bind9 перезапущен."
}

process_device() {
    local ip="$1" mac="$2" vendor="$3"
    local avahi_name netbios_name rdns_name host_name name device_id db_mac

    avahi_name=$(avahi-browse -r -p -t _workstation._tcp | grep "$ip" | awk -F';' '{print $7}' | sed 's/\.local//')
    netbios_name=$(nmblookup -A "$ip" | awk '/<00>/ && !/<GROUP>/ {print $1}' | tr -d '\n')
    rdns_name=$(dig +short -x "$ip" | sed 's/\.$//')
    host_name=$(host "$ip" | awk '{print $5}' | sed 's/\.$//')
    name=$(clean_name "${avahi_name:-${netbios_name:-${rdns_name:-$host_name}}}")

    if [[ -z "$name" ]]; then
        name=$(generate_device_name "$mac" "$vendor" "$ip")
        log_msg "INFO" "Не удалось определить имя, используем сгенерированное: $name ($ip)"
    fi

    read -r device_id db_mac < <(execute_sql "SELECT id, mac FROM devices WHERE mac = '$mac' LIMIT 1;")

    if [[ -n "$device_id" ]]; then
        execute_sql "UPDATE devices SET name = '$name', ip = '$ip', vendor = '$vendor', last_seen = NOW(), connected = TRUE WHERE id = $device_id;"
        execute_sql "INSERT INTO device_history (device_id, event_type) VALUES ($device_id, 'connected');"
        log_msg "INFO" "Обновлено устройство: $name ($ip)"
    else
        execute_sql "INSERT INTO devices (name, ip, mac, vendor, connected, last_seen) VALUES ('$name', '$ip', '$mac', '$vendor', TRUE, NOW());"
        mdevice_id=$(execute_sql "SELECT id FROM devices WHERE mac = '$mac' LIMIT 1;")
        execute_sql "INSERT INTO device_history (device_id, event_type) VALUES ($mdevice_id, 'connected');"
        log_msg "INFO" "Добавлено новое устройство: $name ($ip)"
    fi
}

arp-scan --localnet | awk '/([0-9]{1,3}\.){3}[0-9]{1,3}/ {print $1 "|" $2 "|" $3}' > "$WORK_DIR/network_scan.txt"
nmap -sn "$SUBNET" | awk '/Nmap scan report/{ip=$NF} /MAC Address:/{print ip "|" $3 "|" $4}' >> "$WORK_DIR/network_scan.txt"

sort -u "$WORK_DIR/network_scan.txt" -o "$WORK_DIR/network_scan.txt"

while IFS='|' read -r ip mac vendor; do
    vendor=$(echo "$vendor" | sed 's/[()]//g')
    process_device "$ip" "$mac" "$vendor"
done < "$WORK_DIR/network_scan.txt"

execute_sql "UPDATE devices SET connected = FALSE WHERE last_seen < NOW() - INTERVAL $TIMEOUT_INTERVAL AND connected = TRUE;"
execute_sql "INSERT INTO device_history (device_id, event_type)
    SELECT id, 'disconnected'
    FROM devices
    WHERE last_seen < NOW() - INTERVAL $TIMEOUT_INTERVAL
      AND connected = FALSE
      AND (
          SELECT IFNULL((SELECT event_type FROM device_history WHERE device_id = devices.id ORDER BY event_time DESC LIMIT 1), '') <> 'disconnected'
      );"

update_dns_file

log_msg "INFO" "Сканирование завершено."
