﻿using UnityEngine;
using NewHorizons.Utility;


namespace TheVision.CustomProps
{
    public class TheVision_SolanumVisionResponse : MonoBehaviour
    {
        public NomaiConversationManager _nomaiConversationManager;
        public SolanumAnimController _solanumAnimController;
        public NomaiWallText solanumVisionResponse;
        public OWAudioSource PlayerHeadsetAudioSource;

        public static readonly int MAX_WAIT_FRAMES = 20;

        // Main
        public void WriteMessage()
        {
            // one-time code that runs after waitFrames are up
            _solanumAnimController.OnWriteResponse += (int unused) =>
            {
                _nomaiConversationManager._activeResponseText = solanumVisionResponse;
                _nomaiConversationManager._pendingResponseText = null;
                solanumVisionResponse.Show();
            };
            _solanumAnimController.StartWritingMessage();

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => !_solanumAnimController.isStartingWrite && !solanumVisionResponse.IsAnimationPlaying(), () =>
            {

                _solanumAnimController.StopWritingMessage(gestureToText: false);
                _nomaiConversationManager._state = NomaiConversationManager.State.WatchingSky;
                _solanumAnimController.StopWatchingPlayer();

                // Spawning SolanumCopies and Signals on vision response
                TheVision.Instance.ModHelper.Events.Unity.FireInNUpdates(TheVision.Instance.SpawnOnVisionEnd, 10);
            });
        }
        public void OnVisionStart()
        {
            if (_nomaiConversationManager._activeResponseText != null) _nomaiConversationManager._activeResponseText.Hide();
            _nomaiConversationManager._activeResponseText = null;
            _nomaiConversationManager._pendingResponseText = solanumVisionResponse;

            // Disabling music on QM once the vision is showed
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Volumes/AudioVolume_QM_Music").gameObject.SetActive(false);
        }
        public void OnVisionEnd()
        {
            // flicker 
            var effect = Locator.GetActiveCamera().transform.Find("ScreenEffects/LightFlickerEffectBubble").GetComponent<LightFlickerController>();
            effect.FlickerOffAndOn(offDuration: 6.8f, onDuration: 1f);

            TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                // sfx             
                PlayWindSound();
                PlayStartSound();
                Invoke("PlayImpactSound", 0.5f);
                Invoke("PlayShockSound", 0.5f);
                PlayFadeInSound();
            });

            TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                // wh parameters
                var whiteHoleOptions = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole/AmbientLight").GetComponent<Light>();
                whiteHoleOptions.color = new Color(1, 1, 2, 1);
                whiteHoleOptions.range = 30;
                whiteHoleOptions.intensity = 3;
                whiteHoleOptions.enabled = true;

                // QM White Hole parameters
                var qmWhiteHole = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole").gameObject;
                var qmWhiteHoleLock = qmWhiteHole.AddComponent<MemoryUplinkTrigger>()._lockOnTransform;

                qmWhiteHole.SetActive(true);

                Invoke("ApplyForce", 0.5f);
                Invoke("CameraShaking", 0.5f);

                Invoke("SolanumAnim", 10f);

                Invoke("SolanumAnim2", 25f);

                // Camera lock on target
                var cameraFixedPosition = Locator.GetPlayerTransform().gameObject.GetComponent<PlayerLockOnTargeting>();
                cameraFixedPosition.LockOn(qmWhiteHole.transform, 20f, true, 3f);


                TheVision.Instance.ModHelper.Console.WriteLine("PROJECTION COMPLETE");
                Locator.GetShipLogManager().RevealFact("SOLANUM_PROJECTION_COMPLETE");
                _nomaiConversationManager.enabled = false;

                TheVision.Instance.ModHelper.Events.Unity.FireInNUpdates(WriteMessage, MAX_WAIT_FRAMES);

            });
        }

        // Utility
        public void ApplyForce()
        {
            SearchUtilities.Find("Player_Body/PlayerCamera").GetComponent<PlayerCameraEffectController>().ApplyExposureDamage();

            // Pushing out force for flat version and VR version
            var applyForce = Locator.GetPlayerTransform().gameObject.GetComponent<OWRigidbody>();
            Vector3 pushBack = new Vector3(0f, 0.006f, -0.008f);
            applyForce.AddLocalImpulse(pushBack);
        }
        public void CameraShaking()
        {
            // Camera shaking
            var cameraShaking = Locator.GetActiveCamera().gameObject.AddComponent<CameraShake>();
            StartCoroutine(cameraShaking.Shake(5f, 0.015f));
        }
        public void SolanumAnim()
        {
            var SolanumAnim = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            SolanumAnim.StartWatchingPlayer();
            SolanumAnim.StartConversation();
        }
        public void SolanumAnim2()
        {
            var SolanumAnim = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            SolanumAnim.EndConversation();
            SolanumAnim.StopWatchingPlayer();
        }

        // Sounds and music
        public void PlayWindSound()
        {
            // SFX on QM after Solanumptojection
            PlayerHeadsetAudioSource = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.EyeVortex_LP); // shattering sound 2428 //2697 - station flicker // 2252 - EyeVortex_LP wind // 2005 - electric core
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }
        public void PlayFadeInSound()
        {
            PlayerHeadsetAudioSource = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/NomaiConversation").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.MemoryUplink_Start); // shattering sound 2428 //2697 - station flicker // 2252 -wind // 2005 - electric core // 2460 MemoryUplink_Start
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 10f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayDelayed(4.5f);

        }
        public void PlayImpactSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("Player_Body").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ImpactHighSpeed); // StationFlicker_RW = 2696// 2005 - electric core //502 -ToolFlashlightFlicker
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 10f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayShockSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("Player_Body").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ElectricShock); // StationFlicker_RW = 2696// 2005 - electric core //502 -ToolFlashlightFlicker
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 10f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayStartSound()
        {
            PlayerHeadsetAudioSource = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.GD_IslandLiftedByTornado); // shattering sound 2428 //2697 - station flicker // 2252 -wind // 2005 - electric core // 2011 - GD_IslandLiftedByTornado
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
    }
}