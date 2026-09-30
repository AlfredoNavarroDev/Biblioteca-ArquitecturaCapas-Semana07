# Biblioteca - Arquitectura en Capas (Semana 07)

Backend en capas para un sistema de gestión de biblioteca: préstamos, devoluciones, autores, libros y socios, con reglas de negocio aplicadas antes de tocar la base de datos.

## Proyectos

| Proyecto | Referencias | Responsabilidad |
|---|---|---|
| `Biblioteca.Entidades` | ninguna | POCOs (`Autor`, `Libro`, `Socio`, `Prestamo`, `DetallePrestamo`) y la interfaz `ILibroRepositorio`. Sin acceso a datos ni reglas. |
| `Biblioteca.Datos` | Entidades | Una clase por entidad (`AutorDatos`, `LibroDatos`, `SocioDatos`, `PrestamoDatos`) con ADO.NET puro (`Microsoft.Data.SqlClient`), consultas parametrizadas (`SqlParameter`, nunca concatenadas). Devuelve entidades o `List<T>`, nunca `SqlDataReader`/`DataTable`. |
| `Biblioteca.Negocio` | Datos, Entidades | `LibroNegocio`, `SocioNegocio`, `PrestamoNegocio` validan reglas antes de llamar a Datos. Lanzan `ReglaNegocioException` con mensaje claro ante cualquier regla incumplida. |
| `Lab07` (WPF) | Negocio, Entidades | Proyecto de inicio. Contiene el `App.config` con la cadena de conexión (leída vía `ConfigurationManager`). No referencia `Biblioteca.Datos` ni tipos de `SqlClient`. Las pantallas de mantenimiento, préstamo, devolución y reporte quedan fuera del alcance actual (solo backend). |

### Por qué `ILibroRepositorio` vive en Entidades y no en Negocio

El enunciado original pedía declarar la interfaz en Negocio e implementarla en Datos. Tomado literalmente eso genera una referencia circular (`Datos` necesitaría ver el tipo de `Negocio`, mientras que `Negocio` ya referencia a `Datos`), lo cual rompe el grafo de referencias obligatorio (`Entidades` ← `Datos` ← `Negocio`). Como `Entidades` es la única capa sin dependencias y ya es visible desde ambos lados, la interfaz se definió ahí. `LibroDatos` la implementa y `LibroNegocio` la recibe por constructor, cumpliendo el punto extra sin ciclos.

## Base de datos

Scripts en `Database/`:

- `01_Schema.sql` — crea `BibliotecaDB` y las tablas `Autores`, `Libros`, `Socios`, `Prestamos`, `DetallePrestamo` con sus FKs. `Activo` es `BIT` con default `1`; `ISBN` y `DNI` son únicos; `DetallePrestamo` tiene clave primaria compuesta (`PrestamoId`, `LibroId`) y `FechaDevolucion` permite `NULL`.
- `02_Seed.sql` — datos de prueba: 8 autores, 20 libros, 10 socios y 5 préstamos con su detalle. El socio **Ana Torres** (`SocioId = 1`) queda con **3 libros pendientes** para poder probar el límite de préstamos.

Ejecutar contra LocalDB:

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database/01_Schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database/02_Seed.sql
```

La cadena de conexión usada por la aplicación está en `Lab07/App.config`:

```
Server=(localdb)\MSSQLLocalDB;Database=BibliotecaDB;Trusted_Connection=True;TrustServerCertificate=True;
```

## Reglas de negocio implementadas

**Libros y Socios**
- Inserción, actualización y baja lógica (`Activo = 0`, nunca `DELETE` físico).
- No se permite repetir `ISBN` (Libros) ni `DNI` (Socios).
- No se puede dar de baja un libro o socio con préstamos pendientes.

**Registrar préstamo** (`PrestamoNegocio.RegistrarPrestamoAsync`)
- Un socio no puede superar 3 libros pendientes (cuenta los que ya tiene más los que está por llevarse).
- No se presta un libro sin ejemplares disponibles, ni a socios o libros con `Activo = 0`.
- Cabecera, detalle y descuento de ejemplares se guardan en **una sola transacción** (`PrestamoDatos.RegistrarAsync`); si algo falla, se hace `Rollback` y no se guarda nada.

**Registrar devolución** (`PrestamoNegocio.RegistrarDevolucionAsync`)
- Guarda `FechaDevolucion`, devuelve el ejemplar al stock y calcula la multa por atraso (**S/ 1.50 por día**, constante definida en `PrestamoNegocio`).
- Cuando ya no quedan libros pendientes en el préstamo, su `Estado` pasa a `Devuelto`.

## Build

```bash
dotnet build Lab07.slnx
```

Compila las 4 capas (`Biblioteca.Entidades`, `Biblioteca.Datos`, `Biblioteca.Negocio`, `Lab07`) sin warnings.
