-- BibliotecaDB - esquema
IF DB_ID(N'BibliotecaDB') IS NULL
BEGIN
    CREATE DATABASE BibliotecaDB;
END
GO

USE BibliotecaDB;
GO

IF OBJECT_ID(N'dbo.DetallePrestamo', N'U') IS NOT NULL DROP TABLE dbo.DetallePrestamo;
IF OBJECT_ID(N'dbo.Prestamos', N'U') IS NOT NULL DROP TABLE dbo.Prestamos;
IF OBJECT_ID(N'dbo.Libros', N'U') IS NOT NULL DROP TABLE dbo.Libros;
IF OBJECT_ID(N'dbo.Socios', N'U') IS NOT NULL DROP TABLE dbo.Socios;
IF OBJECT_ID(N'dbo.Autores', N'U') IS NOT NULL DROP TABLE dbo.Autores;
GO

CREATE TABLE dbo.Autores
(
    AutorId       INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(150)   NOT NULL,
    Nacionalidad  NVARCHAR(100)   NOT NULL,
    Activo        BIT             NOT NULL CONSTRAINT DF_Autores_Activo DEFAULT (1)
);
GO

CREATE TABLE dbo.Libros
(
    LibroId     INT IDENTITY(1,1) PRIMARY KEY,
    Titulo      NVARCHAR(200)   NOT NULL,
    ISBN        NVARCHAR(20)    NOT NULL,
    AutorId     INT             NOT NULL,
    Ejemplares  INT             NOT NULL,
    Activo      BIT             NOT NULL CONSTRAINT DF_Libros_Activo DEFAULT (1),
    CONSTRAINT UQ_Libros_ISBN UNIQUE (ISBN),
    CONSTRAINT FK_Libros_Autores FOREIGN KEY (AutorId) REFERENCES dbo.Autores(AutorId),
    CONSTRAINT CK_Libros_Ejemplares CHECK (Ejemplares >= 0)
);
GO

CREATE TABLE dbo.Socios
(
    SocioId INT IDENTITY(1,1) PRIMARY KEY,
    DNI     NVARCHAR(15)    NOT NULL,
    Nombre  NVARCHAR(150)   NOT NULL,
    Email   NVARCHAR(150)   NOT NULL,
    Activo  BIT             NOT NULL CONSTRAINT DF_Socios_Activo DEFAULT (1),
    CONSTRAINT UQ_Socios_DNI UNIQUE (DNI)
);
GO

CREATE TABLE dbo.Prestamos
(
    PrestamoId    INT IDENTITY(1,1) PRIMARY KEY,
    SocioId       INT             NOT NULL,
    FechaPrestamo DATETIME2       NOT NULL,
    FechaLimite   DATETIME2       NOT NULL,
    Estado        NVARCHAR(20)    NOT NULL,
    CONSTRAINT FK_Prestamos_Socios FOREIGN KEY (SocioId) REFERENCES dbo.Socios(SocioId),
    CONSTRAINT CK_Prestamos_Estado CHECK (Estado IN (N'Pendiente', N'Devuelto'))
);
GO

CREATE TABLE dbo.DetallePrestamo
(
    PrestamoId      INT             NOT NULL,
    LibroId         INT             NOT NULL,
    FechaDevolucion DATETIME2       NULL,
    CONSTRAINT PK_DetallePrestamo PRIMARY KEY (PrestamoId, LibroId),
    CONSTRAINT FK_DetallePrestamo_Prestamos FOREIGN KEY (PrestamoId) REFERENCES dbo.Prestamos(PrestamoId),
    CONSTRAINT FK_DetallePrestamo_Libros FOREIGN KEY (LibroId) REFERENCES dbo.Libros(LibroId)
);
GO
