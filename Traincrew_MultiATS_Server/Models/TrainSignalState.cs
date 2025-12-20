using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("train_signal_state")]
public class TrainSignalState
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("train_number")]
    [Required]
    public required string TrainNumber { get; set; }

    [Column("signal_name")]
    [Required]
    public required string SignalName { get; set; }

    // ナビゲーションプロパティ
    [ForeignKey(nameof(SignalName))]
    public Signal? Signal { get; set; }
}
