# Technical Challenge – API de Extracción de Productos

## Descripción
API REST construida con **ASP.NET Core** que permite:
* Gestionar productos (CRUD + actualización parcial vía PATCH).
* Solicitar extracciones de información desde la web **Automation Exercise**.
* Procesar las extracciones de forma **asíncrona** en segundo plano.
* Extraer datos (precio, disponibilidad, marca, categoría, etc.) mediante scraping con **AngleSharp**.
* Consultar el estado y los resultados detallados de cada extracción.
* Manejar fallos parciales: cada producto se procesa individualmente y se registra su estado.

---

## 🚀 Cómo ejecutar el proyecto

### Prerrequisitos
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [SQL Server](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) (puede ser LocalDB, Express o Developer Edition)
* [Visual Studio 2022](https://visualstudio.microsoft.com/es/) (opcional, pero recomendado)
* Git (para clonar)

### Pasos

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/andres-fls/TechnicalChallenge.API.git
   cd TechnicalChallenge.API
   ```

2. **Restaurar paquetes NuGet**
   ```bash
   dotnet restore
   ```

3. **Configurar la cadena de conexión**  
   Edita `appsettings.json` y ajusta el valor de `DefaultConnection` según tu SQL Server. Por defecto usa LocalDB:
   ```json
   "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TechnicalChallengeDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   }
   ```

4. **Crear la base de datos (migraciones)**
   ```bash
   dotnet ef database update
   ```
   *O desde Visual Studio: Herramientas → Administrador de paquetes NuGet → Consola del Administrador de paquetes y ejecutar:* `Update-Database`

5. **Ejecutar la API**
   ```bash
   dotnet run
   ```
   *O presiona F5 desde Visual Studio.*

6. **Acceder a Swagger (documentación interactiva)**  
   Abre tu navegador en:
   `https://localhost:7222/swagger/index.html`  
   *(El puerto puede variar; revisa la consola para confirmar la URL).*

---

## 🧱 Stack tecnológico

| Componente | Tecnología |
| :--- | :--- |
| **Lenguaje** | C# 12 / .NET 10 |
| **Framework** | ASP.NET Core Web API |
| **ORM** | Entity Framework Core (SQL Server) |
| **Base de datos** | SQL Server (LocalDB / Express) |
| **Scraping** | AngleSharp |
| **Documentación API** | Swagger / OpenAPI |
| **Procesamiento en background** | BackgroundService + SemaphoreSlim |
| **Control de versiones** | Git + GitHub |

---

## 📂 Estructura del proyecto

```text
TechnicalChallenge.API/
├── Controllers/
│   ├── ProductsController.cs      # CRUD + PATCH de productos
│   └── ExtractionsController.cs   # POST y GET de extracciones
├── Data/
│   └── AppDbContext.cs            # Configuración de EF Core
├── Dtos/
│   ├── CreateProductDto.cs
│   ├── UpdateProductDto.cs
│   ├── PatchProductDto.cs         # Para actualización parcial (PATCH)
│   ├── ProductResponseDto.cs
│   ├── ExtractionRequestDto.cs
│   ├── ExtractionResponseDto.cs
│   └── ExtractionItemResponseDto.cs
├── Entities/
│   ├── Enums.cs                   # Estados de Extraction y ExtractionItem
│   ├── Product.cs
│   ├── Extraction.cs
│   └── ExtractionItem.cs
├── Services/
│   ├── IScraperService.cs         # Interfaz para scraping
│   └── ScraperService.cs          # Implementación con AngleSharp
├── Background/
│   ├── ExtractionQueue.cs         # Cola en memoria (ConcurrentQueue + SemaphoreSlim)
│   └── ExtractionWorker.cs        # BackgroundService que procesa extracciones
├── Migrations/                    # Archivos generados por EF Core
├── appsettings.json               # Configuración (cadena de conexión)
└── Program.cs                     # Punto de entrada y registro de servicios
```

---

## 🔌 Endpoints principales

### Productos (`/api/Products`)

| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Products` | Listar todos los productos |
| **GET** | `/api/Products/{id}` | Obtener un producto por su ID interno |
| **POST** | `/api/Products` | Crear un nuevo producto |
| **PATCH** | `/api/Products/{id}` | Actualizar parcialmente un producto (ej. solo precio) |
| **DELETE** | `/api/Products/{id}` | Eliminar un producto (solo si no tiene historial de extracciones) |

### Extracciones (`/api/Extractions`)

| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **POST** | `/api/Extractions` | Solicitar una extracción para una lista de productIds. Devuelve `202 Accepted` con el ID de la extracción. |
| **GET** | `/api/Extractions/{id}` | Consultar el estado detallado de una extracción (incluye el estado de cada producto). |

---

## 🧠 Decisiones de diseño clave

* **Separación de `Id` y `ExternalId` en `Product`:**  
  `Id` es la PK interna (autoincremental). `ExternalId` es el identificador en la fuente externa (*Automation Exercise*). Permite actualizar atributos (como precio) sin perder el historial, ya que `ExternalId` es único e inmutable.
* **`ExtractionItem` no duplica datos del producto:**  
  Solo guarda `ProductId` como FK. Evita inconsistencias si el producto se actualiza después. La "verdad" del producto siempre está en la tabla `Product`.
* **Estados como string en la base de datos:**  
  Los enums (`ExtractionStatus`, `ExtractionItemStatus`) se almacenan como texto para mayor legibilidad y facilidad al depurar.
* **Actualización parcial con `PATCH`:**  
  Se implementa usando `PatchProductDto` con propiedades opcionales (`?`). Solo actualiza los campos que el cliente envía en la petición.
* **Procesamiento asíncrono con `BackgroundService`:**  
  Las extracciones se encolan en memoria (`ConcurrentQueue`). El worker consume la cola y procesa cada extracción en segundo plano. Límite de 5 extracciones en paralelo; cada extracción procesa sus productos secuencialmente para respetar la fuente externa.
* **Manejo de fallos parciales:**  
  Cada `ExtractionItem` tiene su propio estado (`Success` / `Failed`). Si un producto falla, la extracción continúa con los demás. El estado final de la extracción será `CompletedWithErrors` si al menos un producto falló.

---

## 📈 Estado actual del proyecto

### ✅ Funcionalidades implementadas
- [x] CRUD completo de Productos con PATCH.
- [x] Creación y consulta de extracciones.
- [x] Scraping real con AngleSharp (ajustable según selectores).
- [x] Procesamiento en segundo plano con cola en memoria.
- [x] Concurrencia limitada (máx 5 procesos simultáneos).
- [x] Documentación Swagger.

### 🔜 Mejoras pendientes (si el tiempo lo permite)
- [ ] Tests unitarios e integración.
- [ ] Dockerizar la aplicación.
- [ ] Logging más detallado.
- [ ] Paginación en la lista de productos.

---

## 🤝 Contribuciones
Este proyecto fue desarrollado como parte de un challenge técnico. No se aceptan PRs externos.

## 📄 Licencia
Este proyecto es de uso exclusivo para fines de evaluación técnica.
