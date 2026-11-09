<#-- Girişteki doğrulama kodu ekranı. Keycloak'ın keycloak.v2/login/login-otp.ftl'inden (26.8) türetildi; farkı: başlık ve
     açıklama, 2 dakikalık geri sayım (js/otp-timer.js) ve "Girişe dön" düğmesi. Üstteki e-posta satırını sayfa düzeni
     (template.ftl) çizer; bu sayfada gise.css gizler. Keycloak yükseltilince üst temadaki dosyayla karşılaştırılmalı. -->
<#import "template.ftl" as layout>
<#import "field.ftl" as field>
<#import "buttons.ftl" as buttons>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('totp'); section>
<!-- template: login-otp.ftl (gise) -->

    <#if section="header">
        ${msg("giseOtpTitle")}
    <#elseif section="form">
        <p class="gise-otp-hint">${msg("giseOtpHint")}</p>
        <form id="kc-otp-login-form" class="${properties.kcFormClass!}" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
            <input id="selectedCredentialId" type="hidden" name="selectedCredentialId" value="${otpLogin.selectedCredentialId!''}">
            <#if otpLogin.userOtpCredentials?size gt 1>
                <div class="${properties.kcFormGroupClass!}">
                    <div class="${properties.kcInputWrapperClass!}">
                        <#list otpLogin.userOtpCredentials as otpCredential>
                            <div id="kc-otp-credential-${otpCredential?index}" class="${properties.kcLoginOTPListClass!}"
                                    onclick="toggleOTP(${otpCredential?index}, '${otpCredential.id}')">
                                <span class="${properties.kcLoginOTPListItemHeaderClass!}">
                                    <span class="${properties.kcLoginOTPListItemIconBodyClass!}">
                                      <i class="${properties.kcLoginOTPListItemIconClass!}" aria-hidden="true"></i>
                                    </span>
                                    <span class="${properties.kcLoginOTPListItemTitleClass!}">${otpCredential.userLabel}</span>
                                </span>
                            </div>
                        </#list>
                    </div>
                </div>
            </#if>

            <@field.input name="otp" label=msg("loginOtpOneTime") autocomplete="one-time-code" fieldName="totp" autofocus=true />

            <#-- Süre yalnızca ekranda: bitince giriş baştan başlar. Kodun kendisini Keycloak doğrular (30 sn'lik pencere). -->
            <p class="gise-otp-timer" role="timer" data-otp-timer data-seconds="120"
               data-restart-url="${url.loginRestartFlowUrl}" data-expired-message="${msg('giseOtpExpired')}">
                ${msg("giseOtpRemaining")} <strong data-remaining>2:00</strong>
            </p>

            <@buttons.loginButton />
        </form>
        <a class="${properties.kcButtonSecondaryClass!} ${properties.kcButtonBlockClass!} gise-otp-back" href="${url.loginRestartFlowUrl}" data-otp-back>${msg("giseOtpBack")}</a>
        <script>
            <#outputformat "JavaScript">
            function toggleOTP(index, value) {
                // select the clicked OTP credential
                document.getElementById("selectedCredentialId").value = value;
                // remove selected class from all OTP credentials
                Array.from(document.getElementsByClassName(${properties.kcLoginOTPListSelectedClass!?c})).map(i => i.classList.remove(${properties.kcLoginOTPListSelectedClass!?c}));
                // add selected class to the clicked OTP credential
                document.getElementById("kc-otp-credential-" + index).classList.add(${properties.kcLoginOTPListSelectedClass!?c});
            }
            </#outputformat>
        </script>
    </#if>
</@layout.registrationLayout>
