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

exec 2>>"$DAILY_LOG"

log_msg() {
    local level="$1"
    local message="$2"
    local timestamp
    timestamp=$(date "+%Y-%m-%d %H:%M:%S")
    echo "$timestamp [$level] - $message" | tee -a "$DAILY_LOG" "$CURRENT_LOG" >/dev/null
}

# Удаляем записи старше 24 часов из current.log
prune_current_log() {
    log_msg "INFO" "Очищаю старые записи в $CURRENT_LOG..."
    local cutoff
    cutoff=$(date -d "24 hours ago" "+%Y-%m-%d %H:%M:%S")

    awk -v cutoff="$cutoff" '
        /^[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2}/ {
            logtime = substr($0, 1, 19)
            if (logtime >= cutoff) print $0
            next
        }
    ' "$CURRENT_LOG" > "$CURRENT_LOG.tmp" || {
        log_msg "ERROR" "Ошибка при обработке $CURRENT_LOG"
        exit 1
    }

    mv "$CURRENT_LOG.tmp" "$CURRENT_LOG"
}
prune_current_log

# Проверка на параллельный запуск
if [[ -f "$LOCK_FILE" ]]; then
    log_msg "WARNING" "Скрипт уже запущен. Выход."
    exit 1
fi
touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"' EXIT

# Проверка .my.cnf
if [[ ! -f "$MY_CNF" ]]; then
    log_msg "ERROR" "Файл конфигурации MySQL ($MY_CNF) не найден. Выход."
    exit 1
fi

# Проверяем наличие необходимых утилит
for util in nmap avahi-browse nmblookup dig mysql timeout host arp-scan; do
    if ! command -v "$util" &>/dev/null; then
        log_msg "ERROR" "Утилита $util не установлена. Выход."
        exit 1
    fi
done

execute_sql() {
    local query="$1"
    log_msg "DEBUG" "SQL: $query"
    if ! result=$(mysql --defaults-extra-file="$MY_CNF" -sN -e "$query" 2>&1); then
        log_msg "ERROR" "SQL-запрос не выполнен: $query; Ошибка: $result"
        echo ""
        return 1
    fi
    echo "$result"
}

# Разрешаем буквы, цифры, тире и точку, переводим в lower
clean_name() {
    echo "$1" | tr -cd '[:alnum:]-.' | tr '[:upper:]' '[:lower:]'
}

# Определяем «плохие» имена
is_bad_name() {
    local nm="$1"
    case "$nm" in
        ""|*nxdomain*|unknown*|nohost*|*invalid*)
            return 0  # плохое
            ;;
        *)
            return 1  # нормальное
            ;;
    esac
}

# Для сокращения некоторых «длинных» производителей, вроде ubiquiti
shorten_manufacturer() {
    local m="$1"
    # Пример простого «словара»:
    if [[ "$m" =~ ubnt|ubiquiti|ubiquitinetworks ]]; then
        echo "ubnt"
    elif [[ "$m" =~ mikrotik ]]; then
        echo "mikrotik"
    elif [[ "$m" =~ intel ]]; then
        echo "intel"
    elif [[ "$m" =~ tp-link ]]; then
        echo "tplink"
    elif [[ "$m" =~ huawei ]]; then
        echo "huawei"
    else
        echo "$m"
    fi
}

# Если ничего не нашлось, fallback = dev
generate_device_name() {
    local mac="$1"
    local manufacturer="$2"
    local ip="$3"

    # Берём последние два октета IP, например, 172.24.0.40 -> "0-40"
    # Если IP почему-то не совпадает с форматом, подстрахуемся
    local short_ip
    short_ip=$(echo "$ip" | awk -F'.' '{print $(NF-1)"-"$NF}') || short_ip="x-x"

    # Берём сокращённый manufacturer
    local m_short
    m_short=$(shorten_manufacturer "$manufacturer")

    [[ -z "$m_short" ]] && m_short="dev"
    # Сформируем "dev-0-40"
    echo "${m_short}-${short_ip}"
}

# Генерируем fallback при конфликте имени
generate_fallback_name() {
    # Параметр: name, ip
    local existing_name="$1"
    local fallback_suffix="$(( $RANDOM % 1000 ))"  # небольшое случайное число
    echo "${existing_name}-${fallback_suffix}"
}

# Склеиваем «Производитель:ОС» (если пусто, оставляем пустое/ dev)
compose_vendor_field() {
    local manufacturer="$1"
    local os_guess="$2"

    # Если manufacturer = unknown или пуст, ставим ""
    if [[ "$manufacturer" == "unknown" ]]; then
        manufacturer=""
    else
        manufacturer=$(shorten_manufacturer "$manufacturer")
    fi

    # Если os_guess = unknown или пуст, ставим ""
    [[ "$os_guess" == "unknown" ]] && os_guess=""

    # Оба известны
    if [[ -n "$manufacturer" && -n "$os_guess" ]]; then
        echo "${manufacturer}:${os_guess}"
    elif [[ -n "$manufacturer" ]]; then
        echo "$manufacturer"
    elif [[ -n "$os_guess" ]]; then
        echo "$os_guess"
    else
        # Если совсем ничего нет
        echo ""
    fi
}

update_dns_file() {
    log_msg "INFO" "Обновление локального DNS файла: $DB_LOCAL"
    local serial
    serial=$(date +%Y%m%d%H)

    {
        echo "\$TTL 3600"
        echo "@ IN SOA ns.local. root.local. ("
        echo "  $serial ; Serial"
        echo "  3600    ; Refresh"
        echo "  86400   ; Retry"
        echo "  2419200 ; Expire"
        echo "  3600 )  ; Negative Cache TTL"
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

        local devices
        devices=$(execute_sql "SELECT name, ip FROM devices WHERE connected = TRUE;") || true
        while IFS=$'\t' read -r dev_name dev_ip; do
            [[ -n "$dev_name" && -n "$dev_ip" ]] && echo "$dev_name IN A $dev_ip"
        done <<< "$devices"
    } > "$DB_LOCAL" || {
        log_msg "ERROR" "Не удалось записать $DB_LOCAL"
        exit 1
    }

    if ! systemctl restart bind9; then
        log_msg "ERROR" "Не удалось перезапустить bind9"
        exit 1
    fi
    log_msg "INFO" "DNS файл обновлен (Serial: $serial), bind9 перезапущен."
}

process_device() {
    local ip="$1"
    local mac="$2"
    local manufacturer_in="$3"
    local os_guess_in="$4"

    log_msg "INFO" "=== Обработка IP=$ip MAC=$mac ==="

    local avahi_name netbios_name rdns_name host_name name vendor
    local device_id db_mac db_ip

    # Попытка получить имя через avahi
    avahi_out=$(avahi-browse -r -p -t _workstation._tcp 2>/dev/null || echo "")
    avahi_name=$(echo "$avahi_out" | grep "$ip" | awk -F';' '{print $7}' | sed 's/\.local//')

    # NetBIOS
    netbios_out=$(nmblookup -A "$ip" 2>/dev/null || echo "")
    netbios_name=$(echo "$netbios_out" | awk '/<00>/ && !/<GROUP>/{print $1}' | tr -d '\n')

    # rDNS / host
    rdns_name=$(dig +short -x "$ip" 2>/dev/null | sed 's/\.$//')
    host_name=$(host "$ip" 2>/dev/null | awk '{print $5}' | sed 's/\.$//')

    # Очищаем manufacturer и os_guess
    local manufacturer
    local os_guess

    manufacturer=$(clean_name "$manufacturer_in")
    os_guess=$(clean_name "$os_guess_in")

    # Генерируем поле vendor
    vendor=$(compose_vendor_field "$manufacturer" "$os_guess")

    # Порядок приоритетов: avahi -> netbios -> rdns -> host
    name=$(clean_name "${avahi_name:-${netbios_name:-${rdns_name:-$host_name}}}")
    if is_bad_name "$name"; then
        name=""
    fi
    # Если имя осталось пустым, генерируем
    if [[ -z "$name" ]]; then
        name=$(generate_device_name "$mac" "$manufacturer" "$ip")
        log_msg "DEBUG" "Имя пустое/неподходящее, сгенерировали: $name"
    fi

    # Ищем устройство по MAC
    row=$(execute_sql "SELECT id, mac, ip FROM devices WHERE mac = '$mac' LIMIT 1;") || row=""
    read -r device_id db_mac db_ip <<< "$row"

    if [[ -n "$device_id" ]]; then
        # Устройство уже есть в БД
        log_msg "DEBUG" "Нашли запись (id=$device_id), проверяем смену IP..."
        if [[ "$db_ip" != "$ip" && -n "$db_ip" ]]; then
            execute_sql "
              INSERT INTO device_history (device_id, event_type, old_ip, new_ip)
              VALUES ($device_id, 'ip_changed', '$db_ip', '$ip')
            " || true
            log_msg "INFO" "Смена IP: $db_ip -> $ip (device_id=$device_id)"
        fi

        log_msg "DEBUG" "Обновляем устройство name=$name ip=$ip vendor=$vendor"
        if ! execute_sql "
          UPDATE devices
          SET name='$name',
              ip='$ip',
              vendor='$vendor',
              connected=TRUE,
              last_seen=NOW()
          WHERE id=$device_id
        "; then
            # Если ошибка (dupe key и т.п.), генерируем fallback
            local fallback
            fallback=$(generate_fallback_name "$name")
            log_msg "WARNING" "Не удалось UPDATE с name=$name. Пробуем fallback=$fallback"
            if ! execute_sql "
              UPDATE devices
              SET name='$fallback',
                  ip='$ip',
                  vendor='$vendor',
                  connected=TRUE,
                  last_seen=NOW()
              WHERE id=$device_id
            "; then
                log_msg "ERROR" "Невозможно обновить device (id=$device_id) ни c $name, ни c fallback=$fallback"
                return
            fi
            name="$fallback"
        fi

        # Ставим 'connected', если не было
        execute_sql "
          INSERT INTO device_history (device_id, event_type)
          SELECT $device_id, 'connected'
          FROM DUAL
          WHERE (
            SELECT event_type
            FROM device_history
            WHERE device_id = $device_id
            ORDER BY event_time DESC
            LIMIT 1
          ) <> 'connected'
          OR NOT EXISTS (
            SELECT 1 FROM device_history WHERE device_id = $device_id
          )
        " || true

        log_msg "INFO" "Обновлён device (id=$device_id): name=$name ip=$ip vendor=$vendor"
    else
        # Новое устройство
        log_msg "DEBUG" "Устройство не найдено в БД, вставляем: name=$name ip=$ip vendor=$vendor"
        if ! execute_sql "
          INSERT INTO devices (name, ip, mac, vendor, connected, last_seen)
          VALUES ('$name', '$ip', '$mac', '$vendor', TRUE, NOW())
        "; then
            # Фолбек, если name уникален
            local fallback
            fallback=$(generate_fallback_name "$name")
            log_msg "WARNING" "Не удалось INSERT, пробуем fallback=$fallback"
            if ! execute_sql "
              INSERT INTO devices (name, ip, mac, vendor, connected, last_seen)
              VALUES ('$fallback', '$ip', '$mac', '$vendor', TRUE, NOW())
            "; then
                log_msg "ERROR" "Не удалось вставить даже с fallback=$fallback, пропускаем."
                return
            fi
            name="$fallback"
        fi

        local new_device_id
        new_device_id=$(execute_sql "SELECT LAST_INSERT_ID();") || true
        if [[ -n "$new_device_id" ]]; then
            execute_sql "
              INSERT INTO device_history (device_id, event_type)
              VALUES ($new_device_id, 'connected')
            " || true

            log_msg "INFO" "Добавлено новое устройство (id=$new_device_id): name=$name ip=$ip vendor=$vendor"
        else
            log_msg "ERROR" "LAST_INSERT_ID() пуст, не удаётся добавить устройство name=$name"
        fi
    fi
}

# =============== Сканирование ===============
log_msg "INFO" "=== Начало сканирования сети $SUBNET ==="

# 1) arp-scan
log_msg "INFO" "Выполняю arp-scan..."
if ! arp_out=$(arp-scan --localnet 2>&1); then
    log_msg "ERROR" "arp-scan неудачен: $arp_out"
    exit 1
fi

# 2) nmap
log_msg "INFO" "Выполняю nmap (определение ОС)..."
if ! nmap_out=$(nmap -n -F -O --osscan-limit --max-os-tries=1 "$SUBNET" 2>&1); then
    log_msg "ERROR" "nmap неудачен: $nmap_out"
    exit 1
fi

log_msg "DEBUG" "Парсим результаты arp-scan => network_scan.txt"
echo "$arp_out" \
  | awk '/([0-9]{1,3}\.){3}[0-9]{1,3}/{ print $1 "|" $2 "|" $3 }' \
  > "$WORK_DIR/network_scan.txt"

log_msg "DEBUG" "Парсим результаты nmap => network_scan.txt (IP|MAC|Manufacturer)"
echo "$nmap_out" \
  | awk '
    /Nmap scan report for/ { ip=$NF }
    /MAC Address:/ {
      mac=$3
      $1=""; $2=""; $3=""
      sub(/^ +/, "", $0)
      sub(/\(/, "", $0)
      sub(/\)/, "", $0)
      print ip "|" mac "|" $0
    }
  ' >> "$WORK_DIR/network_scan.txt"

sort -u "$WORK_DIR/network_scan.txt" -o "$WORK_DIR/network_scan.txt"

log_msg "DEBUG" "Формируем map IP -> OS (nmap_os.txt)"
echo "$nmap_out" \
  | awk '
    /Nmap scan report for/ { ip=$NF; next }
    /Running:/ {
       sub(/^Running:\s+/, "", $0)
       os=$0
       print ip "|" os
    }
  ' > "$WORK_DIR/nmap_os.txt"
sort -u "$WORK_DIR/nmap_os.txt" -o "$WORK_DIR/nmap_os.txt"

declare -A os_map
while IFS='|' read -r os_ip os_line; do
    # Очистим от лишних символов
    os_line=$(clean_name "$os_line")
    os_map["$os_ip"]="$os_line"
done < "$WORK_DIR/nmap_os.txt"

log_msg "INFO" "Обрабатываем результаты: network_scan.txt"
while IFS='|' read -r ip mac manufacturer; do
    [[ -z "$ip" || -z "$mac" ]] && continue
    local_os="${os_map[$ip]:-}"

    # Убираем слишком длинный/неинформативный manufacturer
    manufacturer=$(shorten_manufacturer "$manufacturer")

    process_device "$ip" "$mac" "$manufacturer" "$local_os"
done < "$WORK_DIR/network_scan.txt"

log_msg "INFO" "Помечаем устройства, не появлявшиеся более $TIMEOUT_INTERVAL, как disconnected..."
execute_sql "
  UPDATE devices
  SET connected = FALSE
  WHERE last_seen < NOW() - INTERVAL $TIMEOUT_INTERVAL
    AND connected = TRUE
" || true

log_msg "DEBUG" "Записываем событие disconnected, если последнее не было disconnected..."
execute_sql "
  INSERT INTO device_history (device_id, event_type)
  SELECT id, 'disconnected'
  FROM devices
  WHERE last_seen < NOW() - INTERVAL $TIMEOUT_INTERVAL
    AND connected = FALSE
    AND (
      SELECT IFNULL((
        SELECT event_type
        FROM device_history
        WHERE device_id = devices.id
        ORDER BY event_time DESC
        LIMIT 1
      ), '') <> 'disconnected'
    )
" || true

update_dns_file

log_msg "INFO" "=== Сканирование завершено! ==="
