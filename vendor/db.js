import mysql from 'mysql2'
let sets = {
    host: 'vr.local',
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
    JOIN users u ON s.userid = u.id;`
    const [rows, fields] = await pool.query(qer)
    return rows;
}

export async function take(userid,lapid,kab,timestart){
    const qer = `INSERT INTO story (userid,lapid,kab,timestart) VALUES (${userid},${lapid},${kab},${timestart});`
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