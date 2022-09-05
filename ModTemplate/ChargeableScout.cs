//Special thanks to PacificEngine, as my percentage display code is largely based off of their Cheats Mod code
using OWML.Common;
using OWML.ModHelper;
using UnityEngine.InputSystem;
using UnityEngine;

namespace ChargeableScout
{
    public class ChargeableScout : ModBehaviour
    {
        //Config things
        private float minMultiplier;
        private float maxMultiplier;
        private float secToCharge;
        public static bool kickbackEnabled;

        //Other variables
        public static float launchMultiplier = 1;
        public static bool launcherPrimed = false;
        private ScreenPrompt percentDisplay = new ScreenPrompt("");


        /**
         * On start, print a message and patch the get max launch speed method of the scout launcher
         */
        private void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"In {nameof(ChargeableScout)}!", MessageType.Success);

            //These patches allow the launch multiplier to actually affect the scout speed
            //Make ProbeLauncher.LaunchProbe record the player velocity just before launch
            ModHelper.HarmonyHelper.AddPrefix<ProbeLauncher>(
                "LaunchProbe",
                typeof(Patches),
                nameof(Patches.LauncherSpeedRecorder));

            //Make SurveyorProbe.Launch use an adjusted scout velocity
            ModHelper.HarmonyHelper.AddPrefix<SurveyorProbe>(
                "Launch",
                typeof(Patches),
                nameof(Patches.LaunchSpeedOverride));

            //Make ProbeLauncher.LaunchProbe do proportional pushback
            ModHelper.HarmonyHelper.AddPostfix<ProbeLauncher>(
                "LaunchProbe",
                typeof(Patches),
                nameof(Patches.PushbackInflator));

            //These patches make the probe only fire when the button is released
            //Make it so that ProbeLauncher.LaunchProbe is supressed until the button is released
            ModHelper.HarmonyHelper.AddPrefix<ProbeLauncher>(
                "LaunchProbe",
                typeof(Patches),
                nameof(Patches.LaunchSuppressor));
        }

        /**
         * Check for button presses every frame
         */
        private void Update()
        {
            //Swap min and max if they're reversed
            if(minMultiplier > maxMultiplier)
            {
                float tmp = minMultiplier;
                minMultiplier = maxMultiplier;
                maxMultiplier = tmp;
            }

            //Charge the probe and display percent if it's primed
            if (launcherPrimed)
            {
                //Remove the priming if the launcher is not equipped
                if ((Locator.GetToolModeSwapper()._equippedTool as ProbeLauncher) == null)
                    launcherPrimed = false;

                else
                {
                    //Up the launch multiplier
                    //Special case for a secToCharge of 0
                    if(secToCharge <= 0)
                    {
                        launchMultiplier = maxMultiplier;
                    }
                    else {
                        launchMultiplier += ((maxMultiplier - minMultiplier) / secToCharge) * Time.deltaTime;
                        launchMultiplier = Mathf.Min(launchMultiplier, maxMultiplier);
                    }

                    //Add the prompt if it's not already in the list
                    if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.BottomCenter).Contains(percentDisplay))
                    {
                        Locator.GetPromptManager().AddScreenPrompt(this.percentDisplay, PromptPosition.BottomCenter);
                    }

                    //Update the prompt and make it visible
                    //Special case if max == min
                    float percentCharge;
                    if(maxMultiplier == minMultiplier)
                        percentCharge = 100;
                    else 
                        percentCharge = Mathf.Round((launchMultiplier - minMultiplier) / (maxMultiplier - minMultiplier) * 100);

                    this.percentDisplay.SetText($"Launcher Charge: {percentCharge}%\n ({Mathf.Round(launchMultiplier * 100) / 100})X Force");
                    this.percentDisplay.SetVisibility(true);

                    //Rumble the controller for style
                    RumbleManager.Pulse(0.05f, 0.05f, 0.05f);
                }
            }

            //Uncharge and remove percentage if it's not
            else
            {
                launchMultiplier = minMultiplier;
                this.percentDisplay.SetVisibility(false);
            }

            //Try and fire the scout when the button is released
            //ProbeLauncher launcher = (Locator.GetToolModeSwapper()._equippedTool as ProbeLauncher);
            if (OWInput.IsNewlyReleased(InputLibrary.probeLaunch) && launcherPrimed)
            {
                //do something with tool swapper
                (Locator.GetToolModeSwapper()._equippedTool as ProbeLauncher).LaunchProbe();
            }
        }

        /**
         * Simple configuration behaviour so the settings actually work
         */
        public override void Configure(IModConfig config)
        {
            minMultiplier = config.GetSettingsValue<float>("minMult");
            maxMultiplier = config.GetSettingsValue<float>("maxMult");
            secToCharge = config.GetSettingsValue<float>("timeToCharge");
            kickbackEnabled = config.GetSettingsValue<bool>("scalingPushback");
        }
    }
}