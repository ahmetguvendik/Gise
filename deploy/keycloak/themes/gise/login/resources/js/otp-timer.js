// Doğrulama kodu ekranında 2 dakikalık geri sayım (login-otp.ftl). Süre bitince giriş baştan başlar (Keycloak'ın restart
// adresi) ve giriş sayfasında "süre doldu" yazar. Yanlış kodla sayfa yenilense de süre baştan başlamaz: bitiş anı bu
// sekmenin giriş oturumuna (tab_id) göre sessionStorage'da tutulur. Süre yalnızca ekranda; kodu Keycloak doğrular.
(function () {
  var PREFIX = 'gise.otpDeadline:';
  var EXPIRED = 'gise.otpExpired';

  function storage() {
    try { return window.sessionStorage; } catch (e) { return null; }
  }

  function clearDeadlines(store) {
    if (!store) return;
    for (var i = store.length - 1; i >= 0; i--) {
      var key = store.key(i);
      if (key && key.indexOf(PREFIX) === 0) store.removeItem(key);
    }
  }

  function startTimer(el) {
    var store = storage();
    var seconds = parseInt(el.getAttribute('data-seconds'), 10) || 120;
    var restartUrl = el.getAttribute('data-restart-url');
    var tabId = new URLSearchParams(window.location.search).get('tab_id') || '';
    var key = PREFIX + tabId;
    var deadline = store && tabId ? parseInt(store.getItem(key) || '', 10) : NaN;
    if (!deadline || isNaN(deadline)) {
      deadline = Date.now() + seconds * 1000;
      if (store && tabId) store.setItem(key, String(deadline));
    }
    var out = el.querySelector('[data-remaining]');
    var timer;

    function leave(expired) {
      clearDeadlines(store);
      if (expired && store) store.setItem(EXPIRED, el.getAttribute('data-expired-message') || '1');
    }

    function tick() {
      var left = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
      out.textContent = Math.floor(left / 60) + ':' + String(left % 60).padStart(2, '0');
      el.classList.toggle('is-ending', left <= 20);
      if (left === 0) {
        clearInterval(timer);
        leave(true);
        window.location.replace(restartUrl);
      }
    }

    var back = document.querySelector('[data-otp-back]');
    if (back) back.addEventListener('click', function () { leave(false); });

    timer = setInterval(tick, 1000);
    tick();
  }

  // Giriş sayfasında: süre dolduysa bir kez bilgi ver; eski bitiş anları silinir (yeniden girişte süre baştan başlar).
  function onLoginPage() {
    var store = storage();
    clearDeadlines(store);
    var message = store && store.getItem(EXPIRED);
    if (!message) return;
    store.removeItem(EXPIRED);
    var form = document.getElementById('kc-form-login');
    if (!form) return;
    var alert = document.createElement('div');
    alert.className = 'pf-v5-c-alert pf-m-inline pf-v5-u-mb-md pf-m-info';
    var title = document.createElement('span');
    title.className = 'pf-v5-c-alert__title kc-feedback-text';
    title.textContent = message === '1' ? 'Süre doldu; tekrar giriş yapın.' : message;
    alert.appendChild(title);
    form.parentNode.insertBefore(alert, form);
  }

  function init() {
    var timer = document.querySelector('[data-otp-timer]');
    if (timer) startTimer(timer);
    else if (document.body && document.body.getAttribute('data-page-id') === 'login-login') onLoginPage();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
