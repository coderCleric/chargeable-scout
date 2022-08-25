using UnityEngine;

namespace ChargeableScout
{
    public class Patches : MonoBehaviour
    {
        private static Vector3 savedVelocity;
        private static Vector3 savedVector;

        /**
         * Saves the velocity vector of the rigidbody the probe launcher is attached to
         * (needed for a later calculation)
         */
        public static void LauncherSpeedRecorder()
        {
            savedVelocity = Locator.GetPlayerTransform().GetAttachedOWRigidbody(false).GetVelocity();
        }
        
        /**
         * Manually calculates the velocity of the launched probe, and disables horizon tracking
         */
        public static void LaunchSpeedOverride(ref Vector3 launchVelocity, ref bool horizonTracking)
        {
            launchVelocity = launchVelocity - savedVelocity; //Temporarily remove the body velocity
            launchVelocity = launchVelocity * ChargeableScout.launchMultiplier; //Apply the multiplier
            launchVelocity = launchVelocity + savedVelocity; //Add the body velocity back on
            savedVector = launchVelocity;
            horizonTracking = false;
        }

        /**
         * Applies additional pushback to the player depending on the launch multiplier
         */
        public static void PushbackInflator(ProbeLauncher __instance, ref bool __runOriginal)
        {
            //Auto return if scaling kickback is disabled
            if (!ChargeableScout.kickbackEnabled || !__runOriginal)
                return;

            OWRigidbody attachedOWRigidbody = Locator.GetPlayerTransform().GetAttachedOWRigidbody(false);
            Vector3 velocityChange = (savedVelocity - savedVector) * 0.05f;
            attachedOWRigidbody.AddVelocityChange(velocityChange * (ChargeableScout.launchMultiplier - 1));
        }

        /**
         * Only allow the method to go off if the launch scout button was just released
         */
        public static bool LaunchSuppressor()
        {
            //If the probe launch button was not just released, only prime the probe
            if (!OWInput.IsNewlyReleased(InputLibrary.probeLaunch))
            {
                ChargeableScout.launcherPrimed = true;
                return false;
            }
            ChargeableScout.launcherPrimed = false;
            return true;
        }
    }
}
