# CourtBooking

API REST para reservar canchas deportivas. Construida con .NET 9, MySQL 8.4 y Cloudflare R2 (imágenes). Se despliega en una VM EC2 de AWS con Docker Compose, Terraform y GitHub Actions.

## Contenido

- [Arquitectura](#arquitectura)
- [API](#api)
- [Ejecutar en local](#ejecutar-en-local)
- [Despliegue en AWS](#despliegue-en-aws)

---

## Arquitectura

Clean Architecture en cuatro proyectos:

| Proyecto | Responsabilidad |
|---|---|
| `src/Domain` | Entidades y reglas de negocio: `Court`, `Reservation`, `TimeRange`. Sin dependencias externas. |
| `src/Application` | Casos de uso (`CreateReservation`, `GetAvailability`, `UploadCourtImage`, …) e interfaces. |
| `src/Infrastructure` | EF Core + MySQL, ASP.NET Identity, emisión de JWT, almacenamiento en R2 (cliente S3). |
| `src/Api` | Controllers, autenticación JWT, mapeo de excepciones a `ProblemDetails`. |

Tests en `tests/Domain.Tests` y `tests/Application.Tests`:

```bash
dotnet test CourtBooking.sln
```

Al arrancar, la API:

1. Aplica las migraciones pendientes de EF Core.
2. Crea los roles `Admin` y `Customer` si no existen.
3. Crea el usuario admin a partir de `Admin__Email` / `Admin__Password`, si están definidos y el email no existe.

---

## API

Base: `/api`. Las rutas protegidas exigen `Authorization: Bearer <accessToken>`.

### Autenticación

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `POST` | `/api/auth/register` | Público | Crea un usuario con rol `Customer`. |
| `POST` | `/api/auth/login` | Público | Devuelve un JWT. |
| `GET` | `/api/auth/me` | Autenticado | Devuelve `id`, `email` y `roles` del token. |

```http
POST /api/auth/register
Content-Type: application/json

{ "email": "user@example.com", "password": "Password123" }
```

Respuesta `201`: `{ "id": "<guid>" }`. Si el email ya existe: `409`.

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "user@example.com", "password": "Password123" }
```

Respuesta `200`: `{ "accessToken": "<jwt>" }`. Credenciales incorrectas: `401`.

- La contraseña debe tener mínimo 8 caracteres (más las reglas por defecto de ASP.NET Identity).
- El email se normaliza a minúsculas.
- El token dura 60 minutos (`Jwt:ExpirationMinutes`).

### Canchas

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `POST` | `/api/courts` | `Admin` | Crea una cancha. |
| `GET` | `/api/courts/{id}/availability?date=YYYY-MM-DD` | Público | Franjas ocupadas del día. |
| `POST` | `/api/courts/{id}/images` | `Admin` | Sube una imagen (`multipart/form-data`, campo `file`). |
| `GET` | `/api/courts/{id}/images` | Público | Lista las imágenes con URL firmada. |

```http
POST /api/courts
Authorization: Bearer <token-admin>
Content-Type: application/json

{ "name": "Cancha 1" }
```

Respuesta `201`: `{ "id": "<guid>" }`.

**Disponibilidad.** Si se omite `date`, se usa el día actual en hora de Colombia (`America/Bogota`). Devuelve solo las reservas confirmadas:

```json
{
  "courtId": "<guid>",
  "date": "2026-11-21",
  "occupiedSlots": [
    { "start": "2026-11-21T18:00:00", "end": "2026-11-21T19:00:00" }
  ]
}
```

**Imágenes.**

- Tipos permitidos: `image/jpeg`, `image/png`, `image/webp`.
- Tamaño máximo: 5 MB.
- Máximo 5 imágenes por cancha.
- Se guardan en R2 con la clave `courts/{courtId}/{guid}.{ext}`.
- El `GET` devuelve `[{ "id": "<guid>", "url": "<url-firmada>" }]`. Cada URL expira en 1 hora.

### Reservas

Todas exigen usuario autenticado.

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `POST` | `/api/reservations` | Autenticado | Crea una reserva a nombre del usuario del token. |
| `DELETE` | `/api/reservations/{id}` | Dueño o `Admin` | Cancela la reserva. |

```http
POST /api/reservations
Authorization: Bearer <token>
Content-Type: application/json

{
  "courtId": "<guid>",
  "start": "2026-11-21T18:00:00",
  "end": "2026-11-21T19:00:00"
}
```

Respuesta `201`: `{ "id": "<guid>" }`. `DELETE` responde `204`.

Reglas de negocio:

- `start` debe ser anterior a `end`.
- La cancha debe existir y estar activa.
- No puede solaparse con otra reserva confirmada de la misma cancha. La comprobación corre dentro de una transacción que bloquea la cancha, para evitar reservas dobles concurrentes.
- Solo el dueño o un `Admin` puede cancelar.
- No se puede cancelar una reserva ya cancelada ni una que ya empezó (hora de Colombia).

### Errores

Los errores siguen el formato `ProblemDetails` (RFC 7807):

| Excepción | HTTP |
|---|---|
| `DomainException` (regla de negocio) | `400` |
| `ValidationException` | `400` |
| `InvalidCredentialsException` | `401` |
| `ForbiddenException` | `403` |
| `NotFoundException` | `404` |
| `ConflictException` | `409` |

El documento OpenAPI (`/openapi/v1.json`) solo se expone en el entorno `Development`. Hay ejemplos de peticiones en [src/Api/Api.http](src/Api/Api.http).

---

## Ejecutar en local

Requisitos: .NET 9 SDK, Docker y `dotnet-ef` (solo para crear migraciones).

1. Copia `.env.example` a `.env` y rellena los valores:

   | Variable | Uso |
   |---|---|
   | `DB_ROOT_PASSWORD`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_PORT` | MySQL |
   | `ConnectionStrings__CourtBooking` | Cadena de conexión de la API |
   | `Jwt__Key` | Clave de firma, mínimo 32 bytes (`openssl rand -base64 48`) |
   | `Admin__Email`, `Admin__Password` | Usuario admin que se crea al arrancar |
   | `R2__ServiceUrl`, `R2__AccessKeyId`, `R2__SecretAccessKey`, `R2__BucketName` | Bucket de imágenes en Cloudflare R2 |

   La API no arranca si falta la cadena de conexión, si `Jwt__Key` es corta o si falta alguna variable de R2.

2. Levanta MySQL:

   ```bash
   docker compose --env-file .env up -d
   ```

3. Arranca la API (carga `.env` y ejecuta `dotnet run`):

   ```bash
   ./run.sh
   ```

   Escucha en `http://localhost:5250`. Las migraciones se aplican solas.

4. Para crear una migración nueva:

   ```bash
   ./ef.sh migrations add <Nombre>
   ```

---

## Despliegue en AWS

### Visión general

```
push a main
   │
   ▼
GitHub Actions ── test ──► build imagen ──► ghcr.io/dubersantiago/courtbooking:<sha>
   │
   │ OIDC (sin claves de AWS guardadas)
   ▼
AWS (us-east-2)
   ├── SSM Parameter Store: /courtbooking/env  (.env de producción, SecureString)
   └── EC2 t3.micro "courtbooking-api" (Amazon Linux 2023, VPC por defecto)
          └── docker compose: mysql:8.4 + api   (puerto 80 → 8080)
```

- **Infraestructura:** Terraform en `infra/`. El estado se guarda en un bucket de Cloudflare R2 (`courtbooking-tfstate`) usando el backend `s3`.
- **Autenticación con AWS:** GitHub Actions asume un rol IAM por OIDC. No hay access keys de AWS en el repositorio.
- **Acceso a la VM:** por AWS Systems Manager (SSM). No hay SSH ni puerto 22 abierto.
- **Base de datos:** MySQL corre en un contenedor de la misma VM, con datos en un volumen Docker. Si se destruye la VM, los datos se pierden.

### Recursos que crea Terraform

| Recurso | Detalle |
|---|---|
| `aws_security_group.api` | Entrada solo TCP 80 desde `0.0.0.0/0`. Salida libre. |
| `aws_instance.api` | `t3.micro`, AMI Amazon Linux 2023 más reciente, disco gp3 de 16 GB, IMDSv2 obligatorio, IP pública, perfil IAM `courtbooking-vm`. |

Variables (`infra/variables.tf`): `region` (por defecto `us-east-2`) e `instance_type` (por defecto `t3.micro`).

Outputs: `vpc_id`, `subnet_ids`, `instance_id`, `public_ip`.

Al primer arranque, `infra/user-data.sh`:

1. Crea 2 GB de swap (la VM solo tiene 1 GB de RAM).
2. Instala y activa Docker.
3. Instala el plugin `docker compose`.
4. Crea `/opt/courtbooking`.

### Requisitos previos (fuera de Terraform)

Estos recursos no están definidos en este repositorio y deben existir antes del primer despliegue:

1. **Proveedor OIDC de GitHub y rol IAM** en AWS que GitHub Actions pueda asumir. El rol necesita permisos para:
   - Terraform: EC2 (instancias, security groups, AMIs, VPC/subnets) y `iam:PassRole` sobre `courtbooking-vm`.
   - Despliegue: `ec2:DescribeInstances`, `ssm:PutParameter`, `ssm:SendCommand`, `ssm:GetCommandInvocation`.
2. **Perfil de instancia IAM `courtbooking-vm`** con:
   - La política `AmazonSSMManagedInstanceCore` (para que SSM controle la VM).
   - Permiso `ssm:GetParameter` sobre `/courtbooking/env` (y KMS decrypt si se usa una clave propia).
3. **Bucket R2 `courtbooking-tfstate`** y un token de API de R2 con acceso de lectura/escritura.
4. **Imagen pública en GHCR.** El script de despliegue no hace `docker login`, así que el paquete `ghcr.io/dubersantiago/courtbooking` debe ser público.

### Secrets de GitHub

| Secret | Usado en | Contenido |
|---|---|---|
| `AWS_ROLE_ARN` | Infra, App, AWS check | ARN del rol IAM para OIDC |
| `R2_ACCOUNT_ID` | Infra | ID de la cuenta de Cloudflare |
| `R2_STATE_ACCESS_KEY_ID` | Infra | Access key del token R2 del estado |
| `R2_STATE_SECRET_ACCESS_KEY` | Infra | Secret key del token R2 del estado |
| `APP_ENV_FILE` | App | Contenido completo del `.env` de producción |

`APP_ENV_FILE` lleva las mismas variables que `.env.example`, salvo `ConnectionStrings__CourtBooking` y `DB_PORT`. El compose de producción construye la cadena de conexión apuntando al contenedor `mysql`. Opcional: `API_PORT` (por defecto `80`).

```env
DB_ROOT_PASSWORD=...
DB_NAME=courtbooking
DB_USER=...
DB_PASSWORD=...
Jwt__Key=...
Admin__Email=...
Admin__Password=...
R2__ServiceUrl=https://<account-id>.r2.cloudflarestorage.com
R2__AccessKeyId=...
R2__SecretAccessKey=...
R2__BucketName=...
```

### Workflows

| Workflow | Disparador | Qué hace |
|---|---|---|
| `aws-check.yml` (**AWS check**) | Manual | Ejecuta `aws sts get-caller-identity` para comprobar que OIDC y el rol funcionan. |
| `infra.yml` (**Infra**) | Manual, con opción `apply` o `destroy` | Genera `backend.hcl` con las credenciales de R2, ejecuta `terraform init` y luego `apply` o `destroy`. Tras `apply`, imprime los outputs. |
| `app.yml` (**App**) | Push a `main` o manual | `test` → `image` → `deploy` (detalle abajo). |

Pasos de `app.yml`:

1. **test:** `dotnet build` y `dotnet test` en Release.
2. **image:** construye el `Dockerfile` y publica en GHCR con los tags `latest` y `<sha>`.
3. **deploy:**
   1. Busca la instancia en estado `running` con tag `Name=courtbooking-api`. Si no hay ninguna, termina sin error y deja un aviso.
   2. Guarda `APP_ENV_FILE` en Parameter Store como `/courtbooking/env` (SecureString).
   3. Envía por SSM (`AWS-RunShellScript`) un script que, en `/opt/courtbooking`:
      - descarga `deploy/docker-compose.yaml` del commit desplegado,
      - escribe el `.env` desde Parameter Store y añade `IMAGE_TAG=<sha>`,
      - ejecuta `docker compose pull` y `docker compose up -d`,
      - borra imágenes antiguas.
   4. Espera hasta 10 minutos a que el comando SSM termine.
   5. Comprueba la API desde fuera: llama a `GET /api/courts/00000000-0000-0000-0000-000000000001/images` y espera un `404` (cancha inexistente, la API responde).

Los jobs de despliegue usan `concurrency` para no correr dos despliegues ni dos ejecuciones de Terraform a la vez.

### Primer despliegue

1. Crea los requisitos previos y los secrets descritos arriba.
2. Ejecuta **AWS check** para validar el acceso.
3. Ejecuta **Infra** con `apply`. Anota el `public_ip` de los outputs.
4. Espera unos minutos a que termine `user-data.sh` (instalación de Docker).
5. Ejecuta **App** manualmente (o haz push a `main`).
6. La API queda en `http://<public_ip>`.

A partir de ahí, cada push a `main` despliega automáticamente.

### Apagar la infraestructura

Ejecuta **Infra** con `destroy`. Esto elimina la VM y el security group. **La base de datos MySQL se pierde**, porque vive en un volumen de esa VM. El parámetro `/courtbooking/env` de SSM no lo gestiona Terraform y se conserva.

### Limitaciones actuales

- Solo HTTP en el puerto 80. No hay TLS ni dominio.
- La IP pública cambia cada vez que se recrea la VM.
- MySQL en la misma VM, sin backups.
