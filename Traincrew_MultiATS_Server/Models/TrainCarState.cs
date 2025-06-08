using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models;

[Table("train_car_state")]
public class TrainCarState
{
    [Key, Column("train_number", Order = 0)]
    public string TrainNumber { get; set; }
    [Key, Column("index", Order = 1)]
    public int Index { get; set; }

    [Column("car_model")]
    public string CarModel { get; set; }

    [Column("has_pantograph")]
    public bool HasPantograph { get; set; }

    [Column("has_driver_cab")]
    public bool HasDriverCab { get; set; }

    [Column("has_conductor_cab")]
    public bool HasConductorCab { get; set; }

    [Column("has_motor")]
    public bool HasMotor { get; set; }

    [Column("door_close")]
    public bool DoorClose { get; set; }

    [Column("bc_press")]
    public bool BcPress { get; set; }

    [Column("ampare")]
    public int Ampare { get; set; }
}
