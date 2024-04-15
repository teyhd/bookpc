import mysql from 'mysql2'
let sets = {
    host: 'vr.local',
    //host: '172.24.0.102',
    user: 'teyhd',
    password : '258000',
    database: 'laptop',
    charset : 'utf8mb4_general_ci',
    waitForConnections: true,
    connectionLimit: 50,
    maxIdle: 50, // max idle connections, the default value is the same as `connectionLimit`
    idleTimeout: 60000, // idle connections timeout, in milliseconds, the default value 60000
    queueLimit: 0,
    enableKeepAlive: true,
    keepAliveInitialDelay: 0
}
const pool = mysql.createPool(sets).promise()

export async function auth_user(login, pass){
    const qer = `SELECT * from users WHERE login='${login}' AND pass='${pass}'`
    const [rows, fields] = await pool.query(qer)
    return rows[0];
}
export async function get_users(userid){
    const qer = `SELECT * from users WHERE id=${userid}`
    const [rows, fields] = await pool.query(qer)
    return rows[0];
}
export async function get_status(){
    const qer = `SELECT s.*, u.name
    FROM story s
    JOIN (
        SELECT lapid, MAX(timestart) AS max_timestart
        FROM story
        GROUP BY lapid
    ) latest ON s.lapid = latest.lapid AND s.timestart = latest.max_timestart
    JOIN users u ON s.userid = u.id
    ORDER BY latest.lapid
    ;`
    const [rows, fields] = await pool.query(qer)
    return rows;
}

export async function get_pc(){
    const qer = `SELECT lapid FROM story GROUP BY lapid ORDER BY lapid;`
    const [rows, fields] = await pool.query(qer)
    return rows;
}
export async function setcmd(lapid,cmd){
    const qer = `UPDATE hosts 
    SET cmd=${cmd}
    WHERE lapid=${lapid};`    
    console.log(qer);
    const [rows, fields] = await pool.query(qer)
    return rows;
}
export async function get_info(){
    const qer = `SELECT * FROM hosts ORDER BY lapid;`
    const [rows, fields] = await pool.query(qer)
    let infst = ['Разблокирован','Заблокирвоан','Не известно']
    var cmd = ['Нет команды','Выключить','Перезагрузить','Заблокировать','Выйти из ПК','Обновить ПК','Убить LastSecur']
    for (let i = 0; i < rows.length; i++) {
        rows[i].times = formatUnixTime(rows[i].times)
        rows[i].lock = infst[rows[i].lock];
        rows[i].cmd = cmd[rows[i].cmd];
    }
    return rows;
}
function formatUnixTime(unixTime) {
    // Преобразование Unix time в миллисекунды
    var dateTime = new Date(unixTime * 1000);
    
    // Получение компонентов времени
    var day = dateTime.getDate();
    var month = dateTime.getMonth() + 1; // Месяцы в JavaScript начинаются с 0
    var year = dateTime.getFullYear();
    
    var hours = dateTime.getHours();
    var minutes = dateTime.getMinutes();
    var seconds = dateTime.getSeconds();
    
    // Добавление ведущих нулей при необходимости
    day = (day < 10) ? '0' + day : day;
    month = (month < 10) ? '0' + month : month;
    hours = (hours < 10) ? '0' + hours : hours;
    minutes = (minutes < 10) ? '0' + minutes : minutes;
    seconds = (seconds < 10) ? '0' + seconds : seconds;
    
    // Формирование строки в нужном формате
    var formattedTime = day + '.' + month + ' ' + hours + ':' + minutes + ':' + seconds;
    
    return formattedTime;
    }
export async function get_pc_story(id){
    const qer = `SELECT story.* , users.name FROM story,users WHERE users.id = story.userid AND lapid=${id} ORDER BY timestart;`
    
    const [rows, fields] = await pool.query(qer)
    console.log(rows);
    return rows;
}

export async function take(userid,lapid,kab,timestart){
    let pass = Math.floor(Math.random() * (9*(Math.pow(10,5)))) + (Math.pow(10,5))
    const qer = `INSERT INTO story (userid,lapid,kab,timestart,komm,pass) VALUES (${userid},${lapid},${kab},${timestart},"Замечаний нет",${pass});`
    const [rows, fields] = await pool.query(qer)
    console.dir(rows);
    return rows.insertId;
}

export async function retlap(timestop,komm,lapid,userid){
    const qer = `UPDATE story 
    SET timestop=${timestop}, komm='${komm}' 
    WHERE userid=${userid} AND lapid=${lapid}
    ORDER BY timestart DESC
    LIMIT 1;`    
    console.log(qer);
    const [rows, fields] = await pool.query(qer)
    return rows;
}



console.log(await get_status());