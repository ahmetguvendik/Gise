// Doğrulama kodu alanı (girişteki kod ekranı ve iki aşamalı doğrulama kurulumu): 6 hane, yalnızca rakam, kare kare.
// Gerçek alan tek bir <input> olarak kalır; yapıştırma, telefonun kod önerisi (one-time-code) ve ekran okuyucu onunla
// çalışır. Kareler alanın altına çizilen görüntüdür (aria-hidden). Hane sayısı realm'in otpPolicyDigits ayarıyla aynı.
(function () {
  var DIGITS = 6;

  function enhance(input) {
    var control = input.parentElement;
    if (!control || control.classList.contains('gise-otp')) return;

    input.setAttribute('maxlength', String(DIGITS));
    input.setAttribute('inputmode', 'numeric');
    input.setAttribute('pattern', '[0-9]{' + DIGITS + '}');
    input.setAttribute('autocomplete', 'one-time-code');
    control.classList.add('gise-otp');

    var boxes = document.createElement('div');
    boxes.className = 'gise-otp__boxes';
    boxes.setAttribute('aria-hidden', 'true');
    for (var i = 0; i < DIGITS; i++) {
      var box = document.createElement('span');
      box.className = 'gise-otp__box';
      boxes.appendChild(box);
    }
    control.insertBefore(boxes, input);

    function render() {
      // Rakam dışı karakterler (boşluk, tire: "123 456") atılır, fazlası kesilir.
      var digits = input.value.replace(/\D/g, '').slice(0, DIGITS);
      if (digits !== input.value) input.value = digits;
      var focused = document.activeElement === input;
      for (var i = 0; i < DIGITS; i++) {
        var box = boxes.children[i];
        box.textContent = digits.charAt(i);
        box.classList.toggle('is-active', focused && i === Math.min(digits.length, DIGITS - 1));
      }
    }

    // İmleç hep sonda: kareler soldan sağa dolar, ortadaki bir kareye tıklayınca araya yazılmaz.
    function caretToEnd() {
      var end = input.value.length;
      try { input.setSelectionRange(end, end); } catch (e) { /* bazı tarayıcılar inputmode'lu alanda izin vermez */ }
      render();
    }

    input.addEventListener('input', render);
    input.addEventListener('blur', render);
    input.addEventListener('focus', caretToEnd);
    input.addEventListener('click', caretToEnd);
    input.addEventListener('keyup', render);
    render();
  }

  function init() {
    ['otp', 'totp'].forEach(function (id) {
      var input = document.getElementById(id);
      if (input && input.tagName === 'INPUT') enhance(input);
    });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
