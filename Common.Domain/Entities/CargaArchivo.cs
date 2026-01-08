using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Domain.Enums;

namespace Common.Domain.Entities
{
    [Table("CargaArchivo", Schema = "dbatlantic")]
    public class CargaArchivo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        
        [Required]
        [StringLength(255)]
        public string FileKey { get; set; }

        
        [Required]
        [StringLength(200)]
        public string NombreArchivo { get; set; }

        [Required]
        [StringLength(150)]
        public string Usuario { get; set; }

        [Required]
        [StringLength(10)]
        public string Periodo { get; set; }

        [Required]
        public DateTimeOffset FechaRegistro { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public EstadoCarga Estado { get; set; }

        public DateTime? FechaFin { get; set; }

        [Required] 
        public string MensajeError { get; set; }

        
        /// <summary>
        /// Determina si el trabajo ya está en proceso o ha terminado (OK/Error).
        /// Se usa para evitar el reprocesamiento.
        /// </summary>
        [NotMapped] 
        public bool IsInProcessOrCompleted =>
            Estado == EstadoCarga.EnProceso ||
            Estado == EstadoCarga.Finalizado ||
            Estado == EstadoCarga.Validado ||
            Estado == EstadoCarga.Notificado ||
            Estado == EstadoCarga.Error; 
    }
}