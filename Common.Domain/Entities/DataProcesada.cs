using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Domain.Entities
{
    /// <summary>
    /// Representa una fila de datos procesados desde un archivo Excel.
    /// Cada registro pertenece a una carga masiva (CargaArchivo).
    /// </summary>
    /// 
    [Table("DataProcesada", Schema = "dbatlantic")]
    public class DataProcesada
    {
        /// <summary>
        /// Identificador único del registro.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Código único del producto.
        /// </summary>
        public string CodigoProducto { get; set; } = default!;

        /// <summary>
        /// Nombre del producto.
        /// </summary>
        public string NombreProducto { get; set; } = default!;

        /// <summary>
        /// Precio del producto.
        /// </summary>
        public decimal Precio { get; set; }

        /// <summary>
        /// Cantidad del producto.
        /// </summary>
        public int Cantidad { get; set; }

        /// <summary>
        /// Período al que pertenece la carga.
        /// </summary>
        public string Periodo { get; set; } = default!;

        /// <summary>
        /// Identificador de la carga masiva asociada.
        /// </summary>
        public int CargaArchivoId { get; set; }

        /// <summary>
        /// Fecha de creación del registro.
        /// </summary>
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        
        /// <summary>
        /// Carga masiva a la que pertenece este registro.
        /// </summary>
        public CargaArchivo CargaArchivo { get; set; } = default!;
    }
}
