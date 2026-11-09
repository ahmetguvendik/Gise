<#-- Bütün HTML maillerin çerçevesi. E-posta istemcileri harici CSS ve SVG'yi çoğunlukla göstermez: stiller satır içinde,
     logo metin. Bağlantılar içerik metninden gelir (messages_tr.properties); düğme görünümü <style>'daki .content a ile. -->
<#macro emailLayout>
<html lang="${locale.language}" dir="${(ltr)?then('ltr','rtl')}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="color-scheme" content="light">
<style>
  .content p { margin: 0 0 14px; }
  .content a { display: inline-block; padding: 12px 22px; border-radius: 999px; background: #ff5a1f; color: #1c110a !important; font-weight: 700; text-decoration: none; }
</style>
</head>
<body style="margin:0;padding:0;background:#f6f1ec;">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f6f1ec;padding:32px 12px;">
  <tr>
    <td align="center">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;">
        <tr>
          <td style="padding:0 4px 16px;font-family:Arial,Helvetica,sans-serif;font-size:24px;font-weight:800;letter-spacing:-0.5px;color:#1c110a;">
            <span style="display:inline-block;width:26px;height:18px;margin-right:8px;vertical-align:-2px;border-radius:4px;background:#ff5a1f;"></span>Gişe
          </td>
        </tr>
        <tr>
          <td class="content" style="background:#ffffff;border:1px solid #ede6df;border-radius:22px;padding:28px;font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.5;color:#1c110a;">
            <#nested>
          </td>
        </tr>
        <tr>
          <td style="padding:16px 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:1.5;color:#6b655c;">
            Bu e-posta Gişe hesabınızla ilgili bir işlem için gönderildi. İşlemi siz yapmadıysanız bu e-postayı dikkate almayın; hesabınızda bir şey değişmez.
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
</body>
</html>
</#macro>
