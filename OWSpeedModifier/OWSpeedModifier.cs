using OWML.Common;
using OWML.ModHelper;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

namespace OWSpeedModifier
{
    public class OWSpeedModifier : ModBehaviour
    {
        public static int currentSpeed;

        public static Key ChangeSpeedKey { get; set; } = Key.V;
        public static int TotalSpeeds { get; set; } = 2;
        public static bool ToggleToChangeSpeed { get; set; } = true;
        public static bool BoostResetsSpeedToMaxValue { get; set; } = true;

        private static float Offset => TotalSpeeds == 2 ? 0.19f : 0.0f;
        public static float SpeedMultiplier => (0.2f + Offset) + (0.80f * (currentSpeed - 1.0f) / (TotalSpeeds - 1.0f));
            
        public override void Configure(IModConfig config)
        {
            base.Configure(config);

            BoostResetsSpeedToMaxValue = config.GetSettingsValue<bool>("boostResetsSpeedToMaxValue");
            TotalSpeeds = config.GetSettingsValue<int>("speedLevels");
            ToggleToChangeSpeed = config.GetSettingsValue<bool>("toggleToChangeSpeed");

            // Parse text into UnityEngine.InputSystem.Key Enum
            string changeSpeedKeyFromConfig = config.GetSettingsValue<string>("changeSpeedKey");
            Enum.TryParse(changeSpeedKeyFromConfig, ignoreCase: true, out Key keyValue);
            if (Enum.IsDefined(typeof(Key), keyValue) && keyValue != Key.None)
            {
                config.SetSettingsValue("changeSpeedKey", keyValue.ToString()); // save with correct case
                ChangeSpeedKey = keyValue;
            }
            else
            {
                // Default to 'V' if bad input was given
                config.SetSettingsValue("changeSpeedKey", Key.V.ToString());
            }

            currentSpeed = TotalSpeeds;
        }

        private void Start()
        {
            ModHelper.HarmonyHelper.AddPostfix<OWInput>(nameof(OWInput.GetAxisValue), typeof(Patches), nameof(Patches.PostGetAxisValue));
            ModHelper.HarmonyHelper.AddPostfix<OWInput>(nameof(OWInput.GetValue), typeof(Patches), nameof(Patches.PostGetValue));
            ModHelper.HarmonyHelper.AddPrefix<JetpackThrusterModel>(nameof(JetpackThrusterModel.ActivateBoost), typeof(Patches), nameof(Patches.BoostResetsSpeed));
        }

        private void Update()
        {
            if (Keyboard.current[ChangeSpeedKey].wasPressedThisFrame)
            {
                int remainder = currentSpeed % TotalSpeeds;
                currentSpeed = (remainder <= 0) ? 1 : ++remainder;
            }

            if (Keyboard.current[ChangeSpeedKey].wasReleasedThisFrame && !ToggleToChangeSpeed && TotalSpeeds == 2)
            {
                currentSpeed = TotalSpeeds;
            }
        }

        public class Patches
        {
            public static void PostGetAxisValue(ref Vector2 __result, IInputCommands command, InputMode mask)
            {
                if (OWInput.UsingGamepad())
                    return;

                var allowedInputs = InputMode.Character | InputMode.ShipCockpit | InputMode.LandingCam;
                if ((mask & allowedInputs) == 0)
                    return;

                if (command == InputLibrary.moveXZ)
                {
                    __result.x = Math.Min(__result.x * SpeedMultiplier, 1.0f);
                    __result.y = Math.Min(__result.y * SpeedMultiplier, 1.0f);
                }
            }

            public static void PostGetValue(ref float __result, IInputCommands command)
            {
                if (OWInput.UsingGamepad())
                    return;

                // InputMode will always be InputMode.All

                if (command == InputLibrary.thrustX ||
                    command == InputLibrary.thrustZ ||
                    command == InputLibrary.thrustUp ||
                    command == InputLibrary.thrustDown)
                {
                    __result = Math.Min(__result * SpeedMultiplier, 1.0f);
                }
            }

            public static void BoostResetsSpeed()
            {
                if (BoostResetsSpeedToMaxValue)
                    currentSpeed = TotalSpeeds;
            }
        }

#if DEBUG
        private void OnGUI()
        {
            if (GUIMode.IsHiddenMode()) return;

            var style = new GUIStyle
            {
                fontSize = 10,
                wordWrap = true
            };
            style.normal.textColor = Color.white;

            var sb = new System.Text.StringBuilder();

            Vector2 vectorMoveXZ = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.ShipCockpit | InputMode.LandingCam);
            float vectorThrustX = OWInput.GetValue(InputLibrary.thrustX, InputMode.All);
            float vectorThrustZ = OWInput.GetValue(InputLibrary.thrustZ, InputMode.All);
            float vectorThrustUp = OWInput.GetValue(InputLibrary.thrustUp, InputMode.All);
            float vectorThrustDown = OWInput.GetValue(InputLibrary.thrustDown, InputMode.All);
            InputMode inputMode = OWInput.GetInputMode();

            sb.AppendLine($"IsGamepadEnabled: {OWInput.UsingGamepad()}");
            sb.AppendLine();
            sb.AppendLine($"moveXZ: {vectorMoveXZ.x.ToString("F2")},{vectorMoveXZ.y.ToString("F2")}");
            sb.AppendLine($"thrustX: {vectorThrustX.ToString("F2")}");
            sb.AppendLine($"thrustZ: {vectorThrustZ.ToString("F2")}");
            sb.AppendLine($"thrustUp: {vectorThrustUp.ToString("F2")}");
            sb.AppendLine($"thrustDown: {vectorThrustDown.ToString("F2")}");
            sb.AppendLine($"inputMode: {inputMode}");
            sb.AppendLine();
            sb.AppendLine($"speed level: {currentSpeed}");

            GUI.Label(new Rect(700f, 20f, 300f, 400f), sb.ToString(), style);
        }
#endif
    }
}
