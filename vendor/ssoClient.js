//npm i express express-session axios jsonwebtoken
// ssoClient.js
import { randomBytes } from 'node:crypto';
import axios from "axios";
import jwt from "jsonwebtoken";
import { mlog } from './logs.js'

export function makeSsoClient(opts) {
  const requireLinkedSession = opts.requireLinkedSession ?? ['1', 'true'].includes(process.env.SSO_REQUIRE_LINKED_SESSION?.toLowerCase());
  const fetchImpl = opts.fetchImpl || globalThis.fetch;
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
  const INTROSPECT = `${ssoBase.replace(/\/$/, '')}/introspect`;
  const isOpaqueToken = value => typeof value === 'string' && /^[A-Za-z0-9_-]{43}$/.test(value);

  async function checkSession(req, res, next) {
    const current = req.session;
    // The callback and local logout must be reachable with an expired cookie.
    if (!current?.user?.id || req.path === '/cb' || req.path === '/logout') return next();
    try {
      let active = !current.ssoSid && (!requireLinkedSession || !current.tokens);
      if (current.ssoSid && isOpaqueToken(current.tokens?.refresh_token)) {
        const result = await fetchImpl(INTROSPECT, {
          method: 'POST',
          headers: { authorization: `Basic ${Buffer.from(`${clientId}:${clientSecret}`).toString('base64')}` },
          body: new URLSearchParams({ token: current.tokens.refresh_token, session_only: '1' }),
          signal: AbortSignal.timeout(1800),
          redirect: 'error',
        });
        if (!result.ok) throw new Error('Introspection request failed');
        const session = await result.json();
        active = session.active === true && session.sid === current.ssoSid &&
          String(session.sub) === String(current.user.id);
      }
      if (active) return next();

      await new Promise((resolve, reject) => current.regenerate(error => error ? reject(error) : resolve()));
      res.clearCookie('wherepc', { path: '/' });
      if (req.path.startsWith('/api/') || !['GET', 'HEAD'].includes(req.method)) {
        return res.status(401).json({ error: 'Unauthorized' });
      }
      return next();
    } catch {
      return res.status(503).send('Проверка входа временно недоступна. Попробуйте ещё раз.');
    }
  }

  function ensureAuth(req, res, next) {
    if (req.session?.user) return next();
    const state = randomBytes(16).toString('hex');
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
      if (typeof req.query.code !== 'string' || !req.query.code ||
          typeof req.query.state !== 'string' || !req.query.state || req.query.state !== req.session.state) {
        return res.status(400).send('Не удалось подтвердить вход. Начните вход заново.');
      }

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
      const opaqueRefresh = isOpaqueToken(tok.refresh_token);
      const ssoSid = opaqueRefresh && typeof payload.sid === 'string' && payload.sid ? payload.sid : null;
      if ((requireLinkedSession || opaqueRefresh) && !ssoSid) {
        return res.status(503).send('Не удалось связать вход с порталом. Попробуйте снова.');
      }
      req.session.ssoSid = ssoSid;

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
       // path: "/",
        httpOnly: true,
        sameSite: "none",       // если фронт на другом домене — можно 'none' + secure:true
        secure: false          // true если HTTPS
      });
       res.clearCookie("wherepc", {
        //path: "/",
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

  return { checkSession, ensureAuth, callback, logout, requireRole };
}
