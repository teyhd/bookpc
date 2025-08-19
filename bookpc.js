import {mlog,say} from './vendor/logs.js'
import { fileURLToPath } from 'url';
let test = false
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
export let appDir = __dirname;

process.on('uncaughtException', (err) => {
    mlog('Глобальный косяк приложения!!! ', err.stack);
    }); 

import request from 'request'
import express from 'express'
import exphbs from 'express-handlebars'
import fileUpload from 'express-fileupload'
import session from 'express-session'
import cookieParser from 'cookie-parser'
import path from 'path'
import fs from 'fs-extra'
import axios from 'axios';
import urlencode from 'urlencode';
import 'dotenv/config'
import { makeSsoClient } from "./vendor/ssoClient.js";
import {getActiv, getDevicesStory,getDevicesInfo, setcmd, rtake,get_info,get_status,take,retlap,get_pc,get_pc_story} from './vendor/db.js'

const app = express();
const hbs = exphbs.create({
defaultLayout: 'main',
extname: 'hbs',
helpers: {
    json: function(context) {
    return JSON.stringify(context, null, 2);
    },
    formatDate: function(date) {
        const d = new Date(date);
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = d.getFullYear();
        const hours = String(d.getHours()).padStart(2, '0');
        const minutes = String(d.getMinutes()).padStart(2, '0');
        const seconds = String(d.getSeconds()).padStart(2, '0');
        return `${day}.${month} ${hours}:${minutes}:${seconds}`; // Форматируем дату
    },
    OK: function(){
    i_count = 1
    },
    I_C: function (opts){
    let anso = ''
    for (let i = 0; i < i_count; i++) {
        anso = anso + "I"
    }
    i_count++
    return anso
    },
    PLS: function (a,opts){

        return a+10
        },
    if_eq: function (a, b, opts) {
        if (a == b){ // Or === depending on your needs
            // logman.log(opts);
            return opts.fn(this);
        } else
            return opts.inverse(this);
    },
    if_more: function (a, b, opts) {
    if (a >= b){ // Or === depending on your needs
        // logman.log(opts);
        return opts.fn(this);
        } else
        return opts.inverse(this);
    },
    for: function(from, to, incr, block) {
        var accum = '';
        for(var i = from; i < to; i += incr)
            accum += block.fn(i);
        return accum;
    }
}
});

const TEMPFOLDER = path.join(appDir,'public/temp');

app.engine('hbs', hbs.engine);
app.set('view engine', 'hbs');
app.set('views','views');
app.set('trust proxy', 1); // за Nginx/Traefik/Cloudflare
app.use(session({name: 'wherepc',resave:true,saveUninitialized:false, secret: 'TEyhdsecurservice1345no', cookie: 
  {secure: false, // ⚠️ обязательно false на HTTP!
  httpOnly: true}
}))

if (test){
    app.use(express.static(path.join(appDir, 'public')));
    app.set('views','views');
} else {
    app.use(express.static(path.join('//',appDir, 'public')));
    app.set('views',path.join('//',appDir, 'views'));
}

console.log(path.join(appDir, 'public'));
app.use(express.json()); // для application/json
app.use(cookieParser());
//app.use(fileUpload());


const sso = makeSsoClient({ //process.env.M
  clientId: process.env.CLIENT_ID,
  clientSecret: process.env.CLIENT_SECRET,
  redirectUri: process.env.REDIRECT_URI,
  ssoBase: process.env.SSO_BASE,
  jwtSecret: process.env.JWT_SECRET,  // такой же, как на SSO             // srv_id из таблицы srvs (например 1 = "portal")
});

app.use(fileUpload({
    useTempFiles : true,
    tempFileDir : TEMPFOLDER,
    defCharset: 'utf8',
    defParamCharset: 'utf8'
}));
// Роуты SSO
app.get("/dbg", (req, res) => {
    req.session.usertt = 15
  res.json({
    cookie: req.headers.cookie || null,
    session: req.session || null,
  });
  console.log(req.session);
  console.log("cookie header:", req.headers.cookie);
   
});

app.get("/cb", sso.callback);

app.get("/er", sso.ensureAuth, (req, res) => {
  res.send(`Привет, user=${req.session.user.id}, роли=${req.session.user.right}`);
});
app.get("/wer", sso.ensureAuth, sso.requireRole("admin"), (req, res) => {
  res.send("Только для админов");
});
app.use(async function (req, res, next) {
    let page = req._parsedOriginalUrl.pathname;
    //console.log('Cookie:', req.headers);
    //.log('Session:', req.session);
    let usr = req.session.user || null
    mlog(page,usr ? usr.id : null ,usr ? usr.name : null,req.session.info,req.headers['nip'],getcurip(req.socket.remoteAddress),req.query)
    next();
})
var cook = null
app.get('/data',async (req,res)=>{
    let ans = await axios.get('https://platoniks.ru/', {headers: {
        Cookie: req.headers.cookie
    } })
   // if (cook == null) cook = ans.headers['set-cookie'][0]
   // mlog(cook);
    //console.log(ans);
    res.redirect('https://platoniks.ru/auth')
    console.log(req.headers.cookie);
    res.send(ans.headers)
    //res.send(req.session)
})

app.get('/',sso.ensureAuth,sso.requireRole(1),async (req,res)=>{
    let pc = await get_status()

    var kabs = []

    for (let j = 0; j < 24; j++) {
        kabs[j] = {kab:j}
       // const element = array[j];
    }

    for (let i = 0; i < pc.length; i++) {
        pc[i].startf = formatUnixTime(pc[i].timestart)
        if (pc[i].kab == 0) {
            pc[i].kab = "13 ложе"
        }
        if (pc[i].kab == 30) {
            pc[i].kab = "доме"
        }
        if (req.session.user.id==pc[i].userid){
            pc[i].me = 1
        }
    }
    let name = req.session.user || null
    if (name!=null) name = name.name
    //console.log(pc);
    res.render('index',{
        title: 'Онлайн Платоникс',
        kabs: kabs,
        meid: req.session.user.id,
        name:name,
        pc: pc,
        auth: req.session.user.right,
        role: req.session.user.right
    });

})

app.get('/story',sso.requireRole(1),async (req,res)=>{
    let laps = await get_pc()
    let name = req.session.user || null
    if (name!=null) name = name.name
    res.render('story',{
        title: 'История использования',
        meid: req.session.user.id,
        name:req.session.name,
        laps: laps,
        auth: req.session.user.id,
        role: req.session.user.right
    });
})
app.get('/device-activ/:hour', async (req, res) => {
    try {
        const activ = await getActiv(req.params.hour);
        res.json(activ);
    } catch (error) {
        console.error('Ошибка при получении activ устройства:', error);
        res.status(500).json({ error: 'Ошибка сервера' });
    }
});
app.get('/device-history/:deviceId', async (req, res) => {
    try {
        const history = await getDevicesStory(req.params.deviceId);

        res.json(history);
    } catch (error) {
        console.error('Ошибка при получении истории устройства:', error);
        res.status(500).json({ error: 'Ошибка сервера' });
    }
});

app.get('/devs',sso.requireRole(5), async (req, res) => {
    try {
        // Получаем данные об устройствах из базы данных
        const devices = await getDevicesInfo(); // Предположим, что эта функция возвращает массив устройств

        // Рендерим страницу с данными
        res.render('devs', {
            title: 'Управление устройствами',
            devices: devices, // Передаем устройства для текущей страницы
            totalDevices: devices.length, // Общее количество устройств
            role: req.session.user.right,
            auth: req.session.user.id // Флаг авторизации
        });
    } catch (error) {
        console.error('Ошибка при получении данных об устройствах:', error);
        res.status(500).send('Ошибка сервера');
    }
});

app.get('/ctrl',sso.requireRole(5),async (req,res)=>{
    let laps = await get_info()
    let infst = ['Разблокирован','Заблокирвоан','Не известно']
    var cmd = ['Нет команды','Выключить','Перезагрузить','Заблокировать','Выйти из ПК','Обновить ПК','LastSecurOFF','ВЫКЛ звук','ВКЛ звук','WIN+D','ALT+F4','CTRL+W','АнтиТим']
    let name = req.session.user || null
    if (name!=null) name = name.name
    console.log(laps);
    res.render('ctrl',{
        title: 'Управление ПК',
        cmd: cmd,
        meid: req.session.user.id,
        name:name,
        laps: laps, 
         role: req.session.role,
        auth: req.session.user.id
    });
})

var pcinfo = await get_info()
async function getchg() {
    let pc = await get_info()
    let ans = {}
    for (let i = 0; i < pc.length; i++) {
        for (const sparam in pc[i]) {
            if (pcinfo[i][sparam] != pc[i][sparam]) {
                ans[`${sparam}_${pc[i].id}`] = pc[i][sparam]
            }
        }
    }
    pcinfo = pc
    return ans
}
app.get('/lapchg',async (req,res)=>{
    let t = await getchg()
    res.json(JSON.stringify(t))
   // res.send(t)
})

app.get('/sendcmd',async (req,res)=>{
    let t = await setcmd(req.query.lapid,req.query.cmd)
    res.send("ok")
})

app.get('/getstory',sso.requireRole(5),async (req,res)=>{
    let laps = await get_pc_story(req.query.lapid)
    for (let j = 0; j < laps.length; j++) {
        if (laps[j].kab == 0) {
            laps[j].kab = "13 ложе"
        }
        if (laps[j].kab == 30) {
            laps[j].kab = "доме"
        }
        laps[j].startf = formatUnixTime(laps[j].timestart)
        laps[j].stopf = formatUnixTime(laps[j].timestop)
    }
    let name = req.session.user || null
    if (name!=null) name = name.name
    say(`${name} запросил истрорию ${req.query.lapid}`)
    res.json(JSON.stringify(laps))
  //  res.send(laps)
})
/*
app.get('/auth',async (req,res)=>{
    console.log(req.query);
    if (req.query.pass!=undefined) {
        let ans = await auth_user(req.query.login,req.query.pass);
        mlog(ans);
        if (ans!=undefined){
            req.session.user.name = ans.user.name
            req.session.user.id = ans.id
            req.session.role = ans.role
            res.send('ok')
        } else {
            res.send('nok')
        }
    } else{
        res.render('auth',{
            title: 'Авторизация',
            auth: req.session.user.id
        });
    }
})  
*/
app.get('/take',async (req,res)=>{
    console.log(req.query);
    let resid = await take(req.session.user.id, parseInt(req.query.lapid), parseInt(req.query.kab) ,getCurrentUnixTime())
    console.log(resid);
    say(`${req.session.user.name} взял ноутбук №${req.query.lapid} в каб №${req.query.kab}`)
    res.send({st:"ok",id:resid})
})  

app.get('/rtake',async (req,res)=>{
    console.log(req.query);
    for (let i = 7; i < 16; i++) { 
        setTimeout(thelp, 30 * i,req.session.user.id,req.session.user.name,i)
    }
    
    res.send({st:"ok"})
})  

async function thelp(userid,name,lapid){
    let resid = await rtake(userid, lapid, 2 ,getCurrentUnixTime())
    console.log(resid);
    say(`${name} взял ноутбук №${lapid} в каб №2`) 
}

app.get('/retlap',async (req,res)=>{
    console.log(req.query);
    let resid = await retlap(getCurrentUnixTime(),req.query.komm, parseInt(req.query.lapid), req.session.user.id)
    console.log(resid);
    console.log(req.session.user.id);
    say(`${req.session.user.name} вернул ноутбук №${req.query.lapid}. \n Комментарий:${req.query.komm}`)
    res.send({st:"ok",id:resid})
})  

//app.get("/logout", sso.logout);
// bookpc
app.get('/logout', (req, res) => {
  const returnTo = process.env.BOOKPC_BASE + '/'; // куда вернуться после SSO-логаута
  const ssoLogout = new URL('/logout', process.env.SSO_BASE);
  ssoLogout.searchParams.set('post_logout_redirect_uri', returnTo);
  ssoLogout.searchParams.set('client_id', process.env.CLIENT_ID);

  req.session.destroy(() => {
    // Стираем только СВОЙ cookie (домен bookpc)
    res.clearCookie('wherepc', { path: '/' /* , domain: process.env.COOKIE_DOMAIN (если задавали при set) */ });
    // НЕ пытаемся чистить sso.sid — это другой домен
    res.redirect(ssoLogout.toString());
  });
});

function getcurip(str) {
    let arr = str.split(':');
    arr = arr[arr.length-1];
    return arr;
}

async function start(){
    app.listen(81,()=> {
        mlog('Сервер - запущен')
        say('Сервер учета оборудования - запущен\nПорт:81');
        mlog('Порт:',81);
    })
}
function getCurrentUnixTime() {
    return Math.floor(new Date().getTime() / 1000);
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

await start()