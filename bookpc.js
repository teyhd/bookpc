import {mlog,say} from './vendor/logs.js'
let test = false
var appDir = path.dirname(import.meta.url);
appDir = appDir.split('///')
appDir = appDir[1]
console.log(appDir);

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
import {getActiv, getDevicesStory,getDevicesInfo, setcmd, rtake,get_info,auth_user,get_users,get_status,take,retlap,get_pc,get_pc_story} from './vendor/db.js'

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

if (test){
    app.use(express.static(path.join(appDir, 'public')));
    app.set('views','views');
} else {
    app.use(express.static(path.join('//',appDir, 'public')));
    app.set('views',path.join('//',appDir, 'views'));
}

console.log(path.join(appDir, 'public'));
app.use(cookieParser());
//app.use(fileUpload());
app.use(session({resave:false,saveUninitialized:false, secret: 'keyboard cat', cookie: {  }}))
app.use(fileUpload({
    useTempFiles : true,
    tempFileDir : TEMPFOLDER,
    defCharset: 'utf8',
    defParamCharset: 'utf8'
}));

app.use(async function (req, res, next) {
    let page = req._parsedOriginalUrl.pathname;

    if (page=='/data' || page=='/ctrlqw' || page=='/lapchgqw' || page=='/addmat' || page=='/device-history'  ) {
        next();
        return 1
    }
    if (req.session.name==undefined) {
        if (page!='/auth') {
            res.redirect("/auth")
        } else next();
    } else {
        if (page=='/auth') {
            res.redirect("/")
        } else next();
    }

    mlog(page,req.session.name,req.headers['nip'],req.query)
    
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

app.get('/',async (req,res)=>{
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
        if (req.session.userid==pc[i].userid){
            pc[i].me = 1
        }
    }
    //console.log(pc);
    res.render('index',{
        title: 'Онлайн Платоникс',
        kabs: kabs,
        meid: req.session.userid,
        name:req.session.name,
        pc: pc,
        auth: req.session.userid,
        role: req.session.role
    });

})

app.get('/story',async (req,res)=>{
    let laps = await get_pc()
    res.render('story',{
        title: 'История использования',
        meid: req.session.userid,
        name:req.session.name,
        laps: laps,
        auth: req.session.userid,
        role: req.session.role
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

app.get('/devs', async (req, res) => {
    try {
        // Получаем данные об устройствах из базы данных
        const devices = await getDevicesInfo(); // Предположим, что эта функция возвращает массив устройств

        // Рендерим страницу с данными
        res.render('devs', {
            title: 'Управление устройствами',
            devices: devices, // Передаем устройства для текущей страницы
            totalDevices: devices.length, // Общее количество устройств
            role: req.session.role,
            auth: req.session.userid // Флаг авторизации
        });
    } catch (error) {
        console.error('Ошибка при получении данных об устройствах:', error);
        res.status(500).send('Ошибка сервера');
    }
});

app.get('/ctrl',async (req,res)=>{
    let laps = await get_info()
    let infst = ['Разблокирован','Заблокирвоан','Не известно']
    var cmd = ['Нет команды','Выключить','Перезагрузить','Заблокировать','Выйти из ПК','Обновить ПК','LastSecurOFF','ВЫКЛ звук','ВКЛ звук','WIN+D','ALT+F4','CTRL+W','АнтиТим']

    console.log(laps);
    res.render('ctrl',{
        title: 'Управление ПК',
        cmd: cmd,
        meid: req.session.userid,
        name:req.session.name,
        laps: laps, 
         role: req.session.role,
        auth: req.session.userid
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

app.get('/getstory',async (req,res)=>{
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
    say(`${req.session.name} запросил истрорию ${req.query.lapid}`)
    res.json(JSON.stringify(laps))
  //  res.send(laps)
})

app.get('/auth',async (req,res)=>{
    console.log(req.query);
    if (req.query.pass!=undefined) {
        let ans = await auth_user(req.query.login,req.query.pass);
        mlog(ans);
        if (ans!=undefined){
            req.session.name = ans.name
            req.session.userid = ans.id
            req.session.role = ans.role
            res.send('ok')
        } else {
            res.send('nok')
        }
    } else{
        res.render('auth',{
            title: 'Авторизация',
            auth: req.session.userid
        });
    }
})  

app.get('/take',async (req,res)=>{
    console.log(req.query);
    let resid = await take(req.session.userid, parseInt(req.query.lapid), parseInt(req.query.kab) ,getCurrentUnixTime())
    console.log(resid);
    say(`${req.session.name} взял ноутбук №${req.query.lapid} в каб №${req.query.kab}`)
    res.send({st:"ok",id:resid})
})  

app.get('/rtake',async (req,res)=>{
    console.log(req.query);
    for (let i = 7; i < 16; i++) { 
        setTimeout(thelp, 30 * i,req.session.userid,req.session.name,i)
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
    let resid = await retlap(getCurrentUnixTime(),req.query.komm, parseInt(req.query.lapid), req.session.userid)
    console.log(resid);
    console.log(req.session.userid);
    say(`${req.session.name} вернул ноутбук №${req.query.lapid}. \n Комментарий:${req.query.komm}`)
    res.send({st:"ok",id:resid})
})  

app.get('/logout', function(req, res) {
    mlog( req.session.name,"вышел из системы");
    req.session.auth = null;
    req.session.name = null
    req.session.userid = null
    //res.send('ok');
    console.dir(req.session)
    req.session.save(function (err) {
      if (err) next(err)
      req.session.regenerate(function (err) {
        if (err) next(err)
        res.redirect('/')
      })
    })
})

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