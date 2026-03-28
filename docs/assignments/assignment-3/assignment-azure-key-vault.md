# Inlämningsuppgift: Azure Key Vault med Managed Identity

## Instruktioner

Besvara frågorna nedan baserat på övningen du har genomfört. Svara med egna ord och visa att du förstår varför, inte bara vad.

---

## Del 1: Kodändringar

### Fråga 1

ASP.NET Core laddar konfiguration från flera providrar i en bestämd ordning. I `Program.cs` registreras Azure Key Vault som den **sista** konfigurationsprovidern via `AddAzureKeyVault(...)`.

Förklara hur konfigurationsprecedensen fungerar i ASP.NET Core. Vilka providrar finns, i vilken ordning laddas de, och vad innebär det att en provider registreras senare än en annan? Vad blir den praktiska konsekvensen av att Key Vault registreras sist — ge ett konkret exempel med `MongoDb:ConnectionString`.

### Fråga 2

Konfigurationsnyckeln `MongoDb:ConnectionString` skrivs olika beroende på var den sätts: i appsettings-filer, som miljövariabel, och som hemlighet i Azure Key Vault. Visa hur samma nyckel skrivs i varje kontext, vilken separator som används (`:`, `__`, `--`), och förklara hur ASP.NET Core mappar dem till samma konfigurationsvärde.

### Fråga 3

Du utgår från en fungerande lokal utvecklingsmiljö (Docker MongoDB, inget Key Vault). Lista **alla** ändringar som krävs för att applikationen ska hämta MongoDB-anslutningssträngen från Azure Key Vault i produktion. Inkludera:

- Vilka NuGet-paket som måste läggas till och vad varje paket gör
- Vilka nya filer som måste skapas
- Vilka befintliga filer som måste ändras, och vad specifikt som ändras i varje fil

### Fråga 4

I `appsettings.Production.json` är `MongoDb:ConnectionString` satt till `"Get from Key Vault"`. Varför orsakar inte detta dummyvärde ett fel när Key Vault är aktiverat? Vad skulle hända om `UseAzureKeyVault` var satt till `false` men `UseMongoDb` var satt till `true` i produktion?

---

## Del 2: Managed Identity och Azure

### Fråga 5

Tänk dig att du har en applikation med känsliga uppgifter som databasanslutningssträngar och API-nycklar. Ett första steg är att samla dessa hemligheter i ett centralt system som Azure Key Vault. Men då uppstår ett nytt problem: applikationen behöver nu autentisera sig mot Key Vault — och den autentiseringen kräver i sig någon form av uppgift (credentials).

Beskriv hur managed identity löser detta problem. Hur går man från att hantera lösenord och nycklar till att bli helt **lösenordsfri** (passwordless)?

### Fråga 6

`DefaultAzureCredential` används både i utveckling och produktion utan att koden ändras. Förklara vilken autentiseringsmetod den använder i respektive miljö och varför detta fungerar utan kodändringar.

### Fråga 7

För att ge VM:en åtkomst till Key Vault-hemligheter kör du två kommandon: ett för att aktivera managed identity på VM:en och ett för att tilldela en RBAC-roll. Skriv båda kommandona (du kan använda platsnamn) och förklara vilken roll du tilldelar och varför just den rollen — inte "Key Vault Administrator".

### Fråga 8

I övningen använder vi en **system-assigned** managed identity för VM:en. Vilka egenskaper har en system-assigned managed identity, och varför passar den bra för just detta scenario? Vad skiljer den från en user-assigned managed identity?
