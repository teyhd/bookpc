//npm i express express-session axios jsonwebtoken
// ssoClient.js
import axios from "axios";
import jwt from "jsonwebtoken";
import { mlog } from './logs.js'

export function makeSsoClient(opts) {
  const {
    clientId,
    clientSecret,
    redirectUri,   // например http://localhost:4001/cb
    ssoBase,       // базовый URL твоего портала-SSO, напр. http://localhost:4000/sso
    jwtSecret,     // общий секрет (для HS256 демо)
  } = opts;

  const AUTHZ  = `${ssoBase}/authorize`;
  const TOKEN  = `${ssoBase}/token`;
  const LOGOUT = `${ssoBase}/logout`;

  function ensureAuth(req, res, next) {
    if (req.session?.user) return next();
    const state = Math.random().toString(36).slice(2);
    req.session.state = state;
   // console.log(req.session);
    
    const url = new URL(AUTHZ);
    url.searchParams.set("client_id", clientId);
    url.searchParams.set("redirect_uri", redirectUri);
    url.searchParams.set("response_type", "code");
   // url.searchParams.set("scope", "openid profile email");
    url.searchParams.set("state", state);
    url.searchParams.set("audience", clientId); // ID сервиса из srvs
    
    return req.session.save(() => res.redirect(url.toString()));
  }

  async function callback(req, res, next) {
    /*console.log(req.query);
    console.log(req.session);
    console.log("cookie header:", req.headers.cookie);*/
    try {
      if (req.query.state != req.session.state) return res.status(400).send(`state mismatch ${req.query.state } YY ${req.session.state}`);

      const params = new URLSearchParams({
        grant_type: "authorization_code",
        code: req.query.code,
        client_id: clientId,
        client_secret: clientSecret,
        redirect_uri: redirectUri,
      });

      const r = await axios.post(TOKEN, params.toString(), {
        headers: { "content-type": "application/x-www-form-urlencoded" },
      });

      const tok = r.data;
      const payload = jwt.verify(tok.access_token, jwtSecret);
      console.log(payload);
      // сохраняем в сессии
      req.session.user = {
        id: payload.sub,
        name: payload.name,
        right: getRolesBySrvId(payload.right,2),
        logins: payload.logins, 
        //roles: payload.roles || payload.roles_ids || [],
      };
      req.session.tokens = tok;

      return res.redirect("/");
    } catch (e) {
      return next(e);
    }
  }
    function getRolesBySrvId(roles,srv_id) {
        let found = [];
        try {
            found = roles
            .filter(r => r.srv_id === srv_id)
            .map(r => r.role_id);
        } catch (error) {
            mlog("Нет роли")
            return 0;
        }
    
    if (found.length == 0) return 0;         // если вообще нет ролей
    if (found.length == 1) return found[0];     // если одна роль → число
    return found;                                // если несколько → массив
    }
  async function logout(req, res) {
    req.session.user = null
    req.session.state = null
    req.session.destroy(async () => {
      try { let h = await axios.get(LOGOUT);
        console.log(h.data);
        res.clearCookie("sso.sid", {
        path: "/",
        httpOnly: true,
        sameSite: "none",       // если фронт на другом домене — можно 'none' + secure:true
        secure: false          // true если HTTPS
      });
       res.clearCookie("wherepc", {
        path: "/",
        httpOnly: true,
        sameSite: "none",       // если фронт на другом домене — можно 'none' + secure:true
        secure: false          // true если HTTPS
      });
      
        //req.session.state = Math.random().toString(36).slice(2);
       } catch (_) {}
       
      res.redirect('/')
    });
  }

  // Middleware для проверки конкретной роли
  function requireRole(role) {
    return (req, res, next) => {
      const roles = req.session.user?.right || 0;
      console.log(roles, role);
      if (roles >= role) return next();
      //if (roles == 0) return res.redirect(process.env.SSO_BASE + '/auth');
      return res.redirect(`${process.env.SSO_BASE}/err`)
    };
  }

  return { ensureAuth, callback, logout, requireRole };
}
