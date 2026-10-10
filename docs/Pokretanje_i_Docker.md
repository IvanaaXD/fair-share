# Pokretanje sistema FairShare

Ovaj dokument opisuje dva načina pokretanja: lokalno, bez Dockera (način na koji je sistem razvijan i testiran), i u kontejnerima pomoću Docker Compose-a (pripremljena konfiguracija za postavljanje na server).

## 1. Lokalno pokretanje (bez Dockera)

### Potrebno

- .NET 10 SDK
- PostgreSQL 16 ili noviji (instaliran direktno na računar)
- Node.js 20 ili noviji (za veb aplikaciju)
- alat za migracije: `dotnet tool install --global dotnet-ef`

### Koraci

1. U PostgreSQL-u napraviti praznu bazu `fairshare`.
2. U `Backend/FairShare.API/appsettings.json` podesiti `ConnectionStrings:PostgresConnection`.
3. Tajne podatke (JWT ključ, lozinku e-mail naloga) ne upisivati u `appsettings.json`, nego u User Secrets, koji se ne nalaze u repozitorijumu:

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

5. Pokrenuti API:

   ```
   dotnet run --project FairShare.API
   ```

   Pri prvom pokretanju kreira se administratorski nalog. Swagger UI je dostupan na `https://localhost:<port>/swagger` (samo u Development okruženju).

6. Pokrenuti veb aplikaciju (`Frontend/web`): `npm install`, zatim `npm run dev`. Aplikacija radi na `http://localhost:5173`, a ta adresa je dozvoljena u CORS podešavanjima API-ja.

### Zamjene za servise iz Docker konfiguracije

| Servis u produkciji | Lokalno | Obrazloženje |
|---|---|---|
| Nginx (reverse proxy) | nije potreban | Veb aplikacija se poziva direktno na API, a CORS dozvoljava adresu Vite servera. |
| Redis (keš) | `IMemoryCache` | Keš u memoriji procesa, iza interfejsa `ICacheService`. Prelazak na Redis (ili Garnet, koji koristi isti protokol) zahtijeva samo drugu implementaciju interfejsa. |
| MinIO (skladište fajlova) | lokalni disk | Fajlovi se čuvaju u folderu `FileStorage:RootPath`, iza interfejsa `IFileStorage`, i preuzimaju se isključivo preko API-ja uz provjeru prava pristupa. |
| Ograničavanje broja zahtjeva | ugrađeno u ASP.NET Core | Ne zahtijeva poseban servis; brojači se drže u memoriji. |

Zahvaljujući ovim interfejsima, poslovna logika ne zavisi od toga koja implementacija je u upotrebi.

## 2. Pokretanje u kontejnerima (Docker Compose)

> Napomena: ova konfiguracija je pripremljena za postavljanje na server i nije testirana na razvojnom računaru, jer Docker Desktop zahtijeva više prostora na disku nego što je bilo dostupno.

### Struktura

```
FairShare/
├── docker-compose.yml
├── .env                 (lokalno, nije u repozitorijumu; šablon je .env.example)
├── nginx/nginx.conf
├── Backend/
│   ├── Dockerfile
│   └── FairShare.API, FairShare.Application, FairShare.Domain, FairShare.Infrastructure
└── Frontend/web/dist    (rezultat komande npm run build)
```

### Kontejneri

- **db** – PostgreSQL 17. Podaci se čuvaju u volumenu `pgdata`. Port nije izložen van Docker mreže.
- **api** – FairShare API, izgrađen iz `Backend/Dockerfile` u dvije faze: u prvoj se kod kompajlira pomoću .NET SDK slike, a u drugoj se rezultat kopira u znatno manju sliku koja sadrži samo ASP.NET Core runtime. Aplikacija u kontejneru radi pod korisnikom bez administratorskih prava. Pri pokretanju sama primjenjuje migracije (`Database:MigrateOnStartup`). Otpremljeni fajlovi čuvaju se u volumenu `uploads`.
- **nginx** – jedina tačka ulaza (port 80). Isporučuje izgrađenu React aplikaciju, a zahtjeve na `/api/` prosljeđuje API-ju uz zaglavlja `X-Forwarded-For` i `X-Forwarded-Proto`, tako da API vidi stvarnu IP adresu klijenta (važno za ograničavanje broja zahtjeva po IP adresi).

Podešavanja API-ja se u kontejneru zadaju promjenljivim okruženja (npr. `JwtSettings__Secret` odgovara ključu `JwtSettings:Secret`), a tajne vrijednosti se čitaju iz fajla `.env`.

### Pokretanje

```
cp .env.example .env          # zatim popuniti vrijednosti
cd Frontend/web && npm run build && cd ../..
docker compose up --build -d
```

Aplikacija je dostupna na `http://localhost`, a Swagger UI na `http://localhost/swagger`.

Korisne komande:

```
docker compose logs -f api                              # log API-ja
docker compose exec api cat admin-initial-password.txt  # početna lozinka administratora
docker compose down                                     # zaustavljanje (podaci ostaju)
docker compose down -v                                  # zaustavljanje i brisanje svih podataka
```
