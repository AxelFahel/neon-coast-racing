using UnityEngine;

namespace NeonCoast {
[System.Serializable]
public class VehicleData {
 public string id;
 public string name;
 public string tagline;
 public float topSpeed;           // Base top speed (default ~60)
 public float torqueMult;          // Acceleration torque multiplier
 public float steerAngleMax;       // Low speed max steer angle (default ~31)
 public float steerAngleMin;       // High speed min steer angle (default ~9)
 public float driftNitroMult;      // Nitro recovery rate during drift
 public float handbrakeGrip;       // Sideways friction stiffness during drift (default 0.65)
 public int statSpeed;             // 1-10 display stat
 public int statAccel;             // 1-10 display stat
 public int statHandling;          // 1-10 display stat
 public int statNitro;             // 1-10 display stat
}

[System.Serializable]
public class PaintData {
 public string name;
 public Color color;
 public Color emission;
}

public static class VehicleRegistry {
 public static readonly VehicleData[] Vehicles = new VehicleData[] {
  new VehicleData {
   id = "aster_gt",
   name = "ASTER GT",
   tagline = "ESPECIFICAÇÃO COSTEIRA  //  EQUILIBRADO",
   topSpeed = 60f,
   torqueMult = 1.0f,
   steerAngleMax = 31f,
   steerAngleMin = 9f,
   driftNitroMult = 1.0f,
   handbrakeGrip = 0.65f,
   statSpeed = 7,
   statAccel = 7,
   statHandling = 8,
   statNitro = 7
  },
  new VehicleData {
   id = "valkyrie_apex",
   name = "VALKYRIE APEX",
   tagline = "HIPER INTERCEPTADOR  //  VELOCIDADE MÁXIMA",
   topSpeed = 72f,
   torqueMult = 1.35f,
   steerAngleMax = 26f,
   steerAngleMin = 8f,
   driftNitroMult = 0.8f,
   handbrakeGrip = 0.72f,
   statSpeed = 10,
   statAccel = 9,
   statHandling = 6,
   statNitro = 6
  },
  new VehicleData {
   id = "shinobi_rspec",
   name = "SHINOBI R-SPEC",
   tagline = "ESPECIALISTA EM DRIFT  //  ÁGIL E PRECISO",
   topSpeed = 55f,
   torqueMult = 0.95f,
   steerAngleMax = 36f,
   steerAngleMin = 11f,
   driftNitroMult = 1.8f,
   handbrakeGrip = 0.50f,
   statSpeed = 6,
   statAccel = 7,
   statHandling = 10,
   statNitro = 10
  }
 };

 public static readonly PaintData[] Paints = new PaintData[] {
  new PaintData { name = "Ciano Íon", color = new Color(0.05f, 0.90f, 1.0f), emission = Color.black },
  new PaintData { name = "Coral Neon", color = new Color(1.0f, 0.18f, 0.38f), emission = Color.black },
  new PaintData { name = "Âmbar Solar", color = new Color(1.0f, 0.68f, 0.08f), emission = Color.black },
  new PaintData { name = "Violeta Fantasma", color = new Color(0.72f, 0.20f, 1.0f), emission = Color.black }
 };

 public static int SelectedVehicleIndex {
  get => PlayerPrefs.GetInt("NCR_VehicleIndex", 0);
  set { PlayerPrefs.SetInt("NCR_VehicleIndex", Mathf.Clamp(value, 0, Vehicles.Length - 1)); PlayerPrefs.Save(); }
 }

 public static int SelectedPaintIndex {
  get => PlayerPrefs.GetInt("NCR_PaintIndex", 0);
  set { PlayerPrefs.SetInt("NCR_PaintIndex", Mathf.Clamp(value, 0, Paints.Length - 1)); PlayerPrefs.Save(); }
 }

 public static VehicleData GetSelectedVehicle() => Vehicles[SelectedVehicleIndex];
 public static PaintData GetSelectedPaint() => Paints[SelectedPaintIndex];
}
}
