// SPDX-License-Identifier: BSD-2-Clause
// MidlaUO: модуль распознавания речи (Voice-to-Text / Speech-to-Text) для Android
//
// Использует нативный вспомогательный класс net.midla.uo.VoiceRecognizerHelper.
// На современных устройствах (Pixel 7 и Android 12+) работает нативно прямо на процессоре (on-device offline STT),
// на более старых устройствах (Android 7.0–11) использует быстрый системный облачный сервис Google.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility.Logging;
using UnityEngine;

namespace ClassicUO.MobileUI
{
    public enum VoiceState
    {
        Idle,
        Ready,
        Speaking,
        Processing,
        Error
    }

    internal static class MobileVoiceInput
    {
        public static bool IsListening { get; private set; }
        public static VoiceState CurrentState { get; private set; } = VoiceState.Idle;

        public static event Action<VoiceState> StateChanged;

        private static readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

#if UNITY_ANDROID && !UNITY_EDITOR
        private static VoiceCallbackProxy _callbackProxy;
#endif

        public static void Enqueue(Action action)
        {
            if (action != null)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        public static void Update()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error($"[VoiceInput] Callback execution error: {ex}");
                }
            }
        }

        public static bool IsRecognitionAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var helper = new AndroidJavaClass("net.midla.uo.VoiceRecognizerHelper"))
                {
                    return helper.CallStatic<bool>("isAvailable", activity);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[VoiceInput] isAvailable error: {ex.Message}");
                return false;
            }
#else
            return true;
#endif
        }

        public static bool HasMicrophonePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { var dummy = UnityEngine.Microphone.devices; } catch { }
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
#else
            return true;
#endif
        }

        public static void RequestMicrophonePermission(Action<bool> onResult)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Log.Info("[VoiceInput] Requesting RECORD_AUDIO permission...");
            if (HasMicrophonePermission())
            {
                onResult?.Invoke(true);
                return;
            }

            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => Enqueue(() => {
                Log.Info("[VoiceInput] Permission granted!");
                onResult?.Invoke(true);
            });
            callbacks.PermissionDenied += (perm) => Enqueue(() => {
                Log.Warn("[VoiceInput] Permission denied!");
                onResult?.Invoke(false);
            });
            callbacks.PermissionDeniedAndDontAskAgain += (perm) => Enqueue(() => {
                Log.Warn("[VoiceInput] Permission denied and dont ask again!");
                onResult?.Invoke(false);
            });

            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
#else
            onResult?.Invoke(true);
#endif
        }

        public static void StartListening(World world = null, bool holdMode = false)
        {
            Log.Info($"[VoiceInput] StartListening called (holdMode={holdMode})");
            if (IsListening)
            {
                return;
            }

            world ??= ClassicUO.Client.Game.UO.World;

            if (!IsRecognitionAvailable())
            {
                Log.Warn("[VoiceInput] Speech recognition not available on device");
                if (world != null) GameActions.Print(world, MobileUiController.T("voice_no_support"), 0x22);
                return;
            }

            if (!HasMicrophonePermission())
            {
                Log.Info("[VoiceInput] Microphone permission not yet granted, asking user...");
                RequestMicrophonePermission(granted =>
                {
                    if (granted)
                    {
                        StartListeningInternal(world, holdMode);
                    }
                    else
                    {
                        if (world != null) GameActions.Print(world, MobileUiController.T("voice_no_mic_perm"), 0x22);
                    }
                });
                return;
            }

            StartListeningInternal(world, holdMode);
        }

        private static void StartListeningInternal(World world, bool holdMode)
        {
            IsListening = true;
            CurrentState = VoiceState.Ready;
            StateChanged?.Invoke(CurrentState);

            if (world != null)
            {
                GameActions.Print(world, "🎤 " + MobileUiController.T("voice_ready"), 0x0035);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var helper = new AndroidJavaClass("net.midla.uo.VoiceRecognizerHelper"))
                {
                    _callbackProxy ??= new VoiceCallbackProxy();
                    string lang = MobileUiTranslation.IsRussian ? "ru-RU" : "en-US";
                    helper.CallStatic("start", activity, lang, holdMode, _callbackProxy);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[VoiceInput] StartListening error: {ex}");
                IsListening = false;
                CurrentState = VoiceState.Error;
                StateChanged?.Invoke(CurrentState);
                if (world != null)
                {
                    GameActions.Print(world, MobileUiController.T("voice_error") + ": " + ex.Message, 0x22);
                }
            }
#else
            Enqueue(() =>
            {
                GameActions.Print(world, "[Голос]: симуляция речи...", 0x35);
            });
#endif
        }

        public static void StopListening()
        {
            if (!IsListening)
            {
                return;
            }

            CurrentState = VoiceState.Processing;
            StateChanged?.Invoke(CurrentState);

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var helper = new AndroidJavaClass("net.midla.uo.VoiceRecognizerHelper"))
                {
                    helper.CallStatic("stop");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[VoiceInput] Stop error: {ex.Message}");
            }
#endif
        }

        public static void Cancel()
        {
            IsListening = false;
            CurrentState = VoiceState.Idle;
            StateChanged?.Invoke(CurrentState);

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var helper = new AndroidJavaClass("net.midla.uo.VoiceRecognizerHelper"))
                {
                    helper.CallStatic("cancel");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[VoiceInput] Cancel error: {ex.Message}");
            }
#endif
        }

        public static void ProcessRecognizedSpeech(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return;
            }

            var world = ClassicUO.Client.Game.UO.World;
            if (world == null || !world.InGame)
            {
                return;
            }

            string clean = rawText.Trim().TrimEnd('.', '!', '?');
            string lower = clean.ToLowerInvariant();

            // Нормализация и быстрые голосовые команды UO
            string finalCommand = null;

            switch (lower)
            {
                case "банк":
                case "bank":
                    finalCommand = "bank";
                    break;

                case "стража":
                case "гварды":
                case "гвард":
                case "guards":
                    finalCommand = "guards";
                    break;

                case "купить":
                case "покупка":
                case "вендор купить":
                    finalCommand = "vendor buy";
                    break;

                case "продать":
                case "продажа":
                case "вендор продать":
                    finalCommand = "vendor sell";
                    break;

                case "все за мной":
                case "за мной":
                case "все ко мне":
                case "ко мне":
                    finalCommand = "all follow me";
                    break;

                case "все в атаку":
                case "атака":
                case "фас":
                case "все атака":
                case "в атаку":
                    finalCommand = "all kill";
                    break;

                case "стоять":
                case "все стоять":
                case "стоп":
                    finalCommand = "all stay";
                    break;

                case "охранять":
                case "все охранять":
                case "охрана":
                    finalCommand = "all guard";
                    break;

                case "статус":
                case "инфо":
                    finalCommand = "status";
                    break;

                default:
                    finalCommand = clean;
                    break;
            }

            // Если открыто текстовое поле (поиск в сумке, поле ввода в чате)
            if (UIManager.KeyboardFocusControl is StbTextBox stb && stb.IsEditable && !stb.IsDisposed)
            {
                stb.InvokeTextInput(finalCommand);

                // Если это системный чат — нажимаем Enter для мгновенной отправки
                if (stb == UIManager.SystemChat?.TextBoxControl)
                {
                    stb.InvokeKeyDown(SDL2.SDL.SDL_Keycode.SDLK_RETURN, SDL2.SDL.SDL_Keymod.KMOD_NONE);
                }

                GameActions.Print(world, $"[🎤 {finalCommand}]", 0x35);
                return;
            }

            // Иначе: персонаж говорит голосом над головой
            GameActions.Say(finalCommand);
            GameActions.Print(world, $"[🎤 {finalCommand}]", 0x35);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private class VoiceCallbackProxy : AndroidJavaProxy
        {
            public VoiceCallbackProxy() : base("net.midla.uo.VoiceRecognizerHelper$Callback") { }

            [UnityEngine.Scripting.Preserve]
            public void onReady()
            {
                Enqueue(() =>
                {
                    IsListening = true;
                    CurrentState = VoiceState.Ready;
                    StateChanged?.Invoke(CurrentState);
                });
            }

            [UnityEngine.Scripting.Preserve]
            public void onBeginning()
            {
                Enqueue(() =>
                {
                    CurrentState = VoiceState.Speaking;
                    StateChanged?.Invoke(CurrentState);
                });
            }

            [UnityEngine.Scripting.Preserve]
            public void onEndOfSpeech()
            {
                Enqueue(() =>
                {
                    CurrentState = VoiceState.Processing;
                    StateChanged?.Invoke(CurrentState);
                });
            }

            [UnityEngine.Scripting.Preserve]
            public void onResults(string text)
            {
                Enqueue(() =>
                {
                    IsListening = false;
                    CurrentState = VoiceState.Idle;
                    StateChanged?.Invoke(CurrentState);

                    if (!string.IsNullOrEmpty(text))
                    {
                        ProcessRecognizedSpeech(text);
                    }
                });
            }

            [UnityEngine.Scripting.Preserve]
            public void onError(int errorCode, string errorMessage)
            {
                Enqueue(() =>
                {
                    IsListening = false;

                    // Если таймаут ожидания речи или речь не распознана (игрок просто нажал и промолчал)
                    if (errorCode == 6 || errorCode == 7) // ERROR_SPEECH_TIMEOUT, ERROR_NO_MATCH
                    {
                        CurrentState = VoiceState.Idle;
                        StateChanged?.Invoke(CurrentState);
                        return;
                    }

                    CurrentState = VoiceState.Error;
                    StateChanged?.Invoke(CurrentState);

                    var world = ClassicUO.Client.Game.UO.World;
                    if (world != null)
                    {
                        GameActions.Print(world, $"[🎤 {errorMessage}]", 0x22);
                    }
                });
            }
        }
#endif
    }
}
