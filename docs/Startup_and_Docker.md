# Pokretanje sistema FairShare

Ovaj dokument opisuje dva načina pokretanja: lokalno, bez Dockera (način na koji je sistem razvijan), i cijeli sistem u kontejnerima pomoću Docker Compose-a.

Za slike se u Dockeru koristi SeaweedFS, S3-kompatibilno skladište. MinIO, koji se najčešće koristi za ovu namjenu, od septembra 2026. više ne objavljuje javne Docker slike; pošto API koristi standardni S3 API, skladište se može zamijeniti bez izmjene koda.

## 1. Lokalno pokretanje (bez Dockera)

### Potrebno

- .NET 10 SDK
- PostgreSQL 16 ili noviji (instaliran direktno na računar)
- Node.js 20 ili noviji (za veb aplikaciju)
- alat za migracije: `dotnet tool install --global dotnet-ef`

### Koraci

1. U PostgreSQL-u napraviti praznu bazu `FairShareDb`.
2. U `Backend/FairShare.API/appsettings.json` podesiti `ConnectionStrings:DefaultConnection`.
3. Tajne podatke (JWT ključ, lozinku e-mail naloga, ključeve simulatora plaćanja) ne upisivati u `appsettings.json`, nego u User Secrets, koji se ne nalaze u repozitorijumu:

   ```
   cd Backend
   dotnet user-secrets init --project FairShare.API
   dotnet user-secrets set "JwtSettings:Secret" "<nasumičan niz od najmanje 32 znaka>" --project FairShare.API
   dotnet user-secrets set "EmailSettings:Password" "<app password>" --project FairShare.API
   ```

4. Primijeniti migracije:

   ```
   dotnet ef database update --project FairShare.Infrastructure --startup-project FairShare.API
   ```

5. Pokrenuti simulator plaćanja, pa API (dva prozora, iz foldera `Backend`):

   ```
   dotnet run --project FairShare.PaymentSimulator
   dotnet run --project FairShare.API --launch-profile https
   ```

   Pri prvom pokretanju kreira se administratorski nalog. Swagger UI je dostupan na `https://localhost:7221/swagger` (samo u Development okruženju).

6. Pokrenuti veb aplikaciju (folder `frontend`): `npm install`, zatim `npm run dev`. Aplikacija radi na `http://localhost:5173`.

### Razlike u odnosu na Docker

| Servis u Dockeru | Lokalno | Obrazloženje |
|---|---|---|
| Nginx (HTTPS, reverse proxy) | Vite dev server | Vite prosljeđuje `/api` na API, pa nema CORS problema. |
| Redis (drugi nivo keša) | samo keš u memoriji | Bez `ConnectionStrings:Redis` HybridCache koristi samo memoriju procesa. Kod je isti. |
| SeaweedFS (skladište slika, S3 API) | lokalni disk | `FileStorage:Provider` je `Local`; fajlovi su u folderu `FileStorage:RootPath`. Oba načina su iza interfejsa `IFileStorage`. |
| Mailpit (test SMTP) | Gmail SMTP | Lokalno se mejlovi stvarno šalju preko naloga iz `EmailSettings`. |

## 2. Pokretanje u kontejnerima (Docker Compose)

### Kontejneri

| Kontejner | Slika | Namjena | Dostupno spolja |
|---|---|---|---|
| **web** | `frontend/Dockerfile` (Node → Nginx) | Isporučuje React aplikaciju i prosljeđuje `/api/` API-ju; HTTPS | 80 (preusmjerava na HTTPS), 443 |
| **api** | `Backend/Dockerfile` | FairShare API | ne |
| **db** | `postgres:17-alpine` | baza API-ja | ne |
| **redis** | `redis:7.4-alpine` | drugi nivo keša (HybridCache L2) | ne |
| **storage** | `chrislusf/seaweedfs` | skladište slika računa i profilnih slika (S3 API) | pregled fajlova: `http://localhost:8888` (samo sa ovog računara) |
| **mailpit** | `axllent/mailpit` | hvata sve mejlove koje API pošalje | `http://localhost:8025` (samo sa ovog računara) |
| **payments** | `Backend/FairShare.PaymentSimulator/Dockerfile` | simulator plaćanja | ne |
| **payments-db** | `postgres:17-alpine` | posebna baza simulatora | ne |
| **certs** | `alpine` | jednom napravi samopotpisani sertifikat za `localhost` u `nginx/certs` | – |

Podešavanja API-ja se u kontejneru zadaju promjenljivim okruženja (npr. `JwtSettings__Secret` odgovara ključu `JwtSettings:Secret`), a tajne vrijednosti se čitaju iz fajla `.env`, koji nije u repozitorijumu.

### Prvo pokretanje

1. Napraviti `.env` od šablona i popuniti vrijednosti (PowerShell, u korijenu repozitorijuma):

   ```
   Copy-Item .env.example .env
   ```

   Nasumične ključeve (JWT_SECRET, PAYMENT_API_KEY, PAYMENT_WEBHOOK_SECRET, S3_SECRET_KEY) može napraviti ova komanda, pokrenuta tri puta:

   ```
   [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
   ```

2. Pokrenuti sve:

   ```
   docker compose up --build -d
   ```

   Prvi build traje nekoliko minuta (preuzimaju se .NET, Node i ostale slike).

3. Otvoriti `https://localhost`. Pregledač će upozoriti na sertifikat, jer je samopotpisan; za lokalno testiranje se prihvata („Advanced → Continue“). Sertifikat se može i dodati u pouzdane (Windows: dvoklik na `nginx/certs/fullchain.pem` nije dovoljan za .pem, pa se koristi `certutil -addstore -user Root nginx\certs\fullchain.pem`).

4. Prijava: demo nalozi (npr. `jelena@demo.fairshare.local`, lozinka `Demo1234!`) prave se automatski na praznoj bazi. Početna lozinka administratora:

   ```
   docker compose exec api cat admin-initial-password.txt
   ```

### Korisne adrese i komande

- Aplikacija: `https://localhost`, Swagger UI: `https://localhost/swagger`
- Mejlovi (Mailpit): `http://localhost:8025`
- Slike (SeaweedFS): `http://localhost:8888/buckets/fairshare/`

```
docker compose ps                     # stanje kontejnera
docker compose logs -f api            # log API-ja
docker compose exec redis redis-cli --scan --pattern "fairshare:*"   # ključevi u Redis kešu
docker compose up --build -d api      # ponovo izgraditi i pokrenuti samo API (poslije izmjene koda)
docker compose down                   # zaustavljanje (podaci ostaju)
docker compose down -v                # zaustavljanje i brisanje svih podataka
```
