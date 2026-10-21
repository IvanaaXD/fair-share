# FairShare – testovi opterećenja (Locust)

Locust pokreće mnogo „virtuelnih korisnika“ odjednom. Svaki se prijavi i radi ono što radi pravi korisnik: gleda grupe i salda, dodaje troškove, otvara analitiku. Locust pri tome mjeri:

- koliko zahtjeva u sekundi API obradi;
- koliko traje odgovor (medijan, 95. i 99. percentil);
- koliko je grešaka.

## Šta je u ovom folderu

| Fajl | Čemu služi |
|---|---|
| `locustfile.py` | scenariji (klase `MixedUser`, `CacheUser`, `ConcurrencyUser`, `RateLimitUser`, `ReportUser`) |
| `fairshare_api.py` | zajednički dio: prijava, test nalozi, pronalaženje ruta, podaci koji se šalju |
| `provjera.py` | brza provjera prije testa: da li prijava i sve rute rade |
| `pokreni_nivoe.ps1` | pokreće scenario na 10, 50, 100 i 200 korisnika i pravi tabelu |
| `sazetak.py` | od CSV rezultata pravi tabelu za rad (Markdown) |
| `requirements.txt` | tačna verzija Locusta |

Rezultati idu u `rezultati/` (HTML izvještaji sa graficima, CSV i tabela).

## 1. Instalacija (jednom)

Treba Python 3.10 ili noviji (python.org; pri instalaciji označi „Add python.exe to PATH“). U PowerShellu, iz root foldera repozitorijuma:

```powershell
cd load-tests
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
locust --version
```

Ako `Activate.ps1` javi da je pokretanje skripti zabranjeno, jednom pokreni `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` i probaj ponovo.

Svaki sljedeći put je dovoljno `cd load-tests` i `.\.venv\Scripts\Activate.ps1`. Na početku reda tada piše `(.venv)`. Folder `.venv` se ne stavlja u git.

## 2. Priprema baze: zasebna kopija za testove

Testovi pišu u bazu i mijenjaju postojeće podatke (poravnanja, isti trošak 50 puta…). Zato se ne pokreću na pravoj bazi `FairShareDb`, nego na njenoj kopiji `fairshare_loadtest`. Prava baza se ne dira.

Testovi koriste 7 demo naloga (`jelena@demo.fairshare.local` … `milica@demo.fairshare.local`, lozinka `Demo1234!`). Oni već postoje u `FairShareDb`, pa ih ima i kopija. Ognjen se ne koristi, jer je u demo podacima namjerno blokiran (za prikaz odblokiranja u administraciji).

**Napravi kopiju.** U pgAdminu otvori Query Tool na bazi **`postgres`** (ne na `FairShareDb`). API mora biti zaustavljen. Označi sve i pokreni (F5):

```sql
ALTER DATABASE "FairShareDb" WITH ALLOW_CONNECTIONS false;

SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'FairShareDb' AND pid <> pg_backend_pid();

CREATE DATABASE fairshare_loadtest TEMPLATE "FairShareDb";

ALTER DATABASE "FairShareDb" WITH ALLOW_CONNECTIONS true;
```

`"FairShareDb"` mora biti u dvostrukim navodnicima zbog velikih slova. Ako `CREATE DATABASE` javi grešku, obavezno sama pokreni posljednji red (`ALLOW_CONNECTIONS true`). Inače se ni aplikacija ni pgAdmin ne mogu spojiti na pravu bazu.

**Reset između scenarija koji pišu** (glavni test i konkurentnost): obriši kopiju i napravi je ponovo, pa svaki test kreće od istog stanja:

```sql
DROP DATABASE fairshare_loadtest WITH (FORCE);
-- pa ponovo blok iznad
```

**Kad završiš sa testovima:** `DROP DATABASE fairshare_loadtest WITH (FORCE);`

## 3. Pokretanje API-ja za test

**Ne kroz Visual Studio i ne u Debug modu**, jer debugger i ispis svakog SQL upita višestruko usporavaju API pa bi se mjerilo njih. Zatvori API u Visual Studiju, pa iz foldera `Backend` (gdje je `.sln`) pokreni:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=fairshare_loadtest;Username=postgres;Password=TVOJA_LOZINKA"
dotnet run --project FairShare.API -c Release --launch-profile https -- `
  --RateLimiting:Enabled=false `
  --Logging:LogLevel:Default=Warning `
  --Logging:LogLevel:Microsoft.AspNetCore=Warning `
  --Logging:LogLevel:Microsoft.EntityFrameworkCore=Warning
```

Prvi red usmjeri API na kopiju baze, samo u ovom PowerShell prozoru. Ima prednost nad user-secrets, a kad zatvoriš prozor, API iz Visual Studija opet radi sa pravom bazom.

Šta znače dodaci iza `--` (važe samo za ovo pokretanje, `appsettings.json` se ne mijenja):

- **`RateLimiting:Enabled=false`:** svi virtuelni korisnici dolaze sa istog računara i dijele 8 naloga. Sa limitom od 300 zahtjeva u minuti po korisniku mjerio bi se limit, a ne API. Da limit radi, pokazuje poseban scenario (tačka 6D).
- **`Logging…=Warning`:** bez ispisa svakog zahtjeva i SQL upita u konzolu.

Simulator plaćanja ne treba (testovi ne plaćaju karticom), ali API traži njegove ključeve u user-secrets, kao i inače.

## 4. Provjera prije testa

U drugom PowerShell prozoru (sa `(.venv)`):

```powershell
python provjera.py https://localhost:7221
python provjera.py https://localhost:7221 --pisanje
```

Prva komanda samo čita. Druga doda po jedan lični i grupni trošak i izračuna prijedlog poravnanja. Na kraju mora pisati „Sve je spremno za Locust.“

Skripta sama pronađe da li su rute u jednini ili množini (npr. `/api/budget` ili `/api/budgets`, `/api/group/{id}/settlements` ili `/api/groups/{id}/settlements`) i ispiše koje je našla. Ako nešto ne prođe, pošalji cijeli ispis.

## 5. Dva načina pokretanja

**Sa prozorom**, za gledanje uživo:

```powershell
locust -f locustfile.py MixedUser --host https://localhost:7221
```

Otvori http://localhost:8089, upiši broj korisnika (npr. 50) i „Ramp up“ (npr. 10 u sekundi), pa Start. Kartica „Charts“ prikazuje grafike, „Download Data“ izvještaj.

**Bez prozora**, za rezultate u rad: rezultat se sam snimi u `rezultati/`.

```powershell
locust -f locustfile.py MixedUser --headless --host https://localhost:7221 -u 100 -r 10 -t 2m --csv rezultati/mix_100 --html rezultati/mix_100.html
```

Uvijek navedi **jednu** klasu (`MixedUser`, `CacheUser`…). Bez nje bi Locust pustio sve scenarije zajedno, pa test odmah prekida sa porukom.

## 6. Scenariji

### A) Glavni test: realna mješavina (`MixedUser`)

Otprilike 70% čitanja i 30% pisanja, sa 1–3 s „razmišljanja“ između akcija:

- **čitanje:** spisak grupa, salda grupe, troškovi grupe, poravnanja, lični troškovi, budžeti, analitika, kategorije, broj nepročitanih obavještenja;
- **pisanje:** lični trošak, grupni trošak sa jednakom podjelom (više tabela u jednoj transakciji) i prijedlog poravnanja (algoritam nad grafom dugova).

Napravi novu kopiju baze (tačka 2), pokreni API kao u tački 3, pa:

```powershell
.\pokreni_nivoe.ps1
```

Pokreće 10, 50, 100 i 200 korisnika po 2 minuta, a na kraju ispiše i snimi tabelu `rezultati/mix_tabela.md`. Druga trajanja i nivoi: `.\pokreni_nivoe.ps1 -Trajanje 3m -Nivoi 10,50,100,200,300`.

Nivo na kojem p95 naglo skoči (npr. sa 80 ms na 900 ms) je tačka zasićenja tvog laptopa. To je zanimljiv podatak za rad.

### B) Keš (`CacheUser`)

Iste analitike iznova. Pokreće se dva puta, a razlika u vremenima je dokaz koliko HybridCache pomaže.

1. API kao u tački 3 (keš uključen):
   ```powershell
   .\pokreni_nivoe.ps1 -Scenario CacheUser -Naziv kes_ukljucen -Nivoi 50,100
   ```
2. Zaustavi API i pokreni ga isto, ali sa još jednim dodatkom `--Caching:Enabled=false`:
   ```powershell
   .\pokreni_nivoe.ps1 -Scenario CacheUser -Naziv kes_iskljucen -Nivoi 50,100
   ```
3. Uporedi `rezultati/kes_ukljucen_tabela.md` i `rezultati/kes_iskljucen_tabela.md`.

### C) Konkurentnost (`ConcurrencyUser`)

50 korisnika istovremeno mijenja **isti** grupni trošak (svi kao Jelena, koja ga je platila). Ispravno ponašanje:

- svaki zahtjev ili uspije (200) ili bude odbijen sa 409 („podatak je izmijenjen u međuvremenu“);
- **nijedan 500**;
- trošak ostaje ispravan.

Napravi novu kopiju baze (tačka 2), pokreni API kao u tački 3, pa:

```powershell
locust -f locustfile.py ConcurrencyUser --headless --host https://localhost:7221 -u 50 -r 50 -t 1m --html rezultati/konkurentnost.html
```

Na kraju se ispiše koliko je bilo 200, a koliko 409, i da li je bilo grešaka 5xx. Za dokaz da je trošak ostao ispravan, u pgAdminu pokreni upit koji traži trošak čiji se udjeli ne slažu sa iznosom. Mora vratiti **0 redova**:

```sql
SELECT ge."Id", ge."Amount", SUM(s."Amount") AS "SumOfSplits"
FROM "GroupExpenses" ge
JOIN "ExpenseSplits" s ON s."GroupExpenseId" = ge."Id"
GROUP BY ge."Id", ge."Amount"
HAVING ge."Amount" <> SUM(s."Amount");
```

### D) Ograničenje prijava (`RateLimitUser`)

Pokušaji prijave sa pogrešnom lozinkom. Poslije 10 pokušaja u minuti API odgovara sa 429, i to je zaštita od pogađanja lozinki. **API mora biti pokrenut normalno, sa limitima**: bez `--RateLimiting:Enabled=false`, može i iz Visual Studija.

```powershell
locust -f locustfile.py RateLimitUser --headless --host https://localhost:7221 -u 1 -r 1 -t 30s
```

Na kraju piše npr. „HTTP 401: 10, HTTP 429: 50 – Ograničenje radi.“ Narednih minut se ni ti ne možeš prijaviti sa tog računara, i to je očekivano.

### E) PDF izvještaji (`ReportUser`)

PDF je namjerno težak za procesor, pa je ovo samo provjera stabilnosti sa nekoliko korisnika:

```powershell
locust -f locustfile.py ReportUser --headless --host https://localhost:7221 -u 5 -r 1 -t 2m --html rezultati/izvjestaji.html
```

## 7. Šta ide u rad

- **Okruženje:** procesor, RAM, Windows, .NET 10 u Release modu, PostgreSQL. Napomena da Locust i API rade na istom laptopu i dijele procesor, pa su rezultati konzervativni. Zašto je rate limiting isključen za glavni test, a posebno testiran.
- **Tabela** iz `rezultati/mix_tabela.md`: korisnici, zahtjevi u sekundi, medijan, p95, p99, greške.
- **Grafici** iz `rezultati/mix_100.html` (ili 200): „Total Requests per Second“ i „Response Times“. Otvoriš HTML u pregledaču i napraviš snimak ekrana.
- **Keš:** dvije tabele jedna pored druge (uključen / isključen) i rečenica o razlici.
- **Konkurentnost:** broj 200/409, 0 grešaka 5xx, upit iz 6C koji vraća 0 redova.
- **Rate limiting:** broj 429 iz scenarija D.

## 8. Kasnije, sa Dockerom

Isti testovi, samo druga adresa: Nginx umjesto API-ja direktno. Npr. `--host https://localhost`. Ograničenje i keš se tada podešavaju kroz promjenljive okruženja u `docker-compose.yml` (`RateLimiting__Enabled=false`, `Caching__Enabled=false`).

## Podešavanja bez izmjene koda

| Promjenljiva | Značenje | Podrazumijevano |
|---|---|---|
| `FS_USERS` | e-mail adrese test naloga, odvojene zarezom | 7 demo naloga |
| `FS_PASSWORD` | njihova lozinka | `Demo1234!` |
| `FS_VERIFY_TLS` | `1` = provjeravaj HTTPS sertifikat | ne provjerava (razvojni sertifikat) |

Primjer u PowerShellu: `$env:FS_USERS = "jelena@demo.fairshare.local,marko@demo.fairshare.local"`.
