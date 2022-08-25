using UnityEngine;
using OWML.ModHelper;

namespace ChargeableScout
{
    public class Patches : MonoBehaviour
    {
        private static Vector3 savedVelocity;
        private static Vector3 savedVector;
        private static bool isStaticLaunch = false;

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
            //Don't use the override speed if this is a static launch
            if (isStaticLaunch)
            {
                isStaticLaunch = false;
                return;
            }

            //Otherwise, calculate the new speed
            savedVector = launchVelocity;
            launchVelocity = launchVelocity - savedVelocity; //Temporarily remove the body velocity
            launchVelocity = launchVelocity * ChargeableScout.launchMultiplier; //Apply the multiplier
            launchVelocity = launchVelocity + savedVelocity; //Add the body velocity back on
            horizonTracking = false;
        }

        /**
         * Applies additional pushback to the player depending on the launch multiplier
         */
        public static void PushbackInflator(ref bool __runOriginal, ProbeLauncher __instance)
        {
            //Auto return if scaling kickback is disabled, the launch was skipped, or the launch was blocked
            if (!ChargeableScout.kickbackEnabled || !__runOriginal || __instance.IsLaunchObstructed())
                return;

            //Otherwise, apply the extra pushback
            OWRigidbody attachedOWRigidbody = Locator.GetPlayerTransform().GetAttachedOWRigidbody(false);
            Vector3 velocityChange = (savedVelocity - savedVector) * 0.05f;
            attachedOWRigidbody.AddVelocityChange(velocityChange * (ChargeableScout.launchMultiplier - 1));
        }

        /**
         * Only allow the method to go off if the launch scout button was just released
         */
        public static bool LaunchSuppressor(ProbeLauncher __instance)
        {
            //Auto unprime and launch if this is a stationary launcher
            if(__instance as StationaryProbeLauncher != null)
            {
                ChargeableScout.launcherPrimed = false;
                isStaticLaunch = true;
                return true;
            }

            //If the probe launch button was not just released, only prime the probe
            if (!OWInput.IsNewlyReleased(InputLibrary.probeLaunch))
            {
                ChargeableScout.launcherPrimed = true;
                return false;
            }

            //Otherwise, unprime and launch
            ChargeableScout.launcherPrimed = false;
            return true;
        }
    }
}
