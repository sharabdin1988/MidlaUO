// SPDX-License-Identifier: BSD-2-Clause
// MidlaUO: модуль распознавания речи (Voice-to-Text / Speech-to-Text) для Android
//
// Использует системный android.speech.SpeechRecognizer.
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

    public static class MobileVoiceInput
    {
        public static bool IsListening { get; private set; }
        public static VoiceState CurrentState { get; private set; } = VoiceState.Idle;

        public static event Action<VoiceState> StateChanged;

        private static readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _speechRecognizer;
        private static SpeechRecognitionListener _listener;
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
                using (var speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                {
                    return speechClass.CallStatic<bool>("isRecognitionAvailable", activity);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[VoiceInput] isRecognitionAvailable error: {ex.Message}");
                return false;
            }
#else
            return true;
#endif
        }

        public static bool HasMicrophonePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
#else
            return true;
#endif
        }

        public static void RequestMicrophonePermission(Action<bool> onResult)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (HasMicrophonePermission())
            {
                onResult?.Invoke(true);
                return;
            }

            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => Enqueue(() => onResult?.Invoke(true));
            callbacks.PermissionDenied += (perm) => Enqueue(() => onResult?.Invoke(false));
            callbacks.PermissionDeniedAndDontAskAgain += (perm) => Enqueue(() => onResult?.Invoke(false));

            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
#else
            onResult?.Invoke(true);
#endif
        }

        public static void StartListening(World world)
        {
            if (IsListening)
            {
                return;
            }

            if (!IsRecognitionAvailable())
            {
                GameActions.Print(world, MobileUiController.T("voice_no_support"), 0x22);
                return;
            }

            if (!HasMicrophonePermission())
            {
                RequestMicrophonePermission(granted =>
                {
                    if (granted)
                    {
                        StartListeningInternal(world);
                    }
                    else
                    {
                        GameActions.Print(world, MobileUiController.T("voice_no_mic_perm"), 0x22);
                    }
                });
                return;
            }

            StartListeningInternal(world);
        }

        private static void StartListeningInternal(World world)
        {
            IsListening = true;
            CurrentState = VoiceState.Ready;
            StateChanged?.Invoke(CurrentState);

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            if (_speechRecognizer == null)
                            {
                                using (var speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                                {
                                    _speechRecognizer = speechClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", activity);
                                    _listener = new SpeechRecognitionListener();
                                    _speechRecognizer.Call("setRecognitionListener", _listener);
                                }
                            }

                            using (var intent = new AndroidJavaObject("android.content.Intent", "android.speech.action.RECOGNIZE_SPEECH"))
                            {
                                intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form");
                                string lang = MobileUiTranslation.IsRussian ? "ru-RU" : "en-US";
                                intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", lang);
                                intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.PARTIAL_RESULTS", false);
                                intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.MAX_RESULTS", 3);

                                _speechRecognizer.Call("startListening", intent);
                            }
                        }
                        catch (Exception ex)
                        {
                            Enqueue(() =>
                            {
                                IsListening = false;
                                CurrentState = VoiceState.Error;
                                StateChanged?.Invoke(CurrentState);
                                GameActions.Print(world, MobileUiController.T("voice_error") + ": " + ex.Message, 0x22);
                            });
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                IsListening = false;
                CurrentState = VoiceState.Error;
                StateChanged?.Invoke(CurrentState);
                GameActions.Print(world, MobileUiController.T("voice_error") + ": " + ex.Message, 0x22);
            }
#else
            // Симуляция в Unity Editor для тестов
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
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            _speechRecognizer?.Call("stopListening");
                        }
                        catch { }
                    }));
                }
            }
            catch { }
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
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            _speechRecognizer?.Call("cancel");
                        }
                        catch { }
                    }));
                }
            }
            catch { }
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

        private static void HandleError(int error)
        {
            var world = ClassicUO.Client.Game.UO.World;
            if (world == null) return;

            switch (error)
            {
                case 6: // ERROR_SPEECH_TIMEOUT
                case 7: // ERROR_NO_MATCH
                    // Тихо завершаем, если игрок промолчал
                    break;
                case 9: // ERROR_INSUFFICIENT_PERMISSIONS
                    GameActions.Print(world, MobileUiController.T("voice_no_mic_perm"), 0x22);
                    break;
                default:
                    Log.Warn($"[VoiceInput] Speech recognizer error code: {error}");
                    break;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private class SpeechRecognitionListener : AndroidJavaProxy
        {
            public SpeechRecognitionListener() : base("android.speech.RecognitionListener") { }

            [UnityEngine.Scripting.Preserve]
            public void onReadyForSpeech(AndroidJavaObject @params)
            {
                Enqueue(() =>
                {
                    IsListening = true;
                    CurrentState = VoiceState.Ready;
                    StateChanged?.Invoke(CurrentState);
                });
            }

            [UnityEngine.Scripting.Preserve]
            public void onBeginningOfSpeech()
            {
                Enqueue(() =>
                {
                    CurrentState = VoiceState.Speaking;
                    StateChanged?.Invoke(CurrentState);
                });
            }

            [UnityEngine.Scripting.Preserve]
            public void onRmsChanged(float rmsdB) { }

            [UnityEngine.Scripting.Preserve]
            public void onBufferReceived(byte[] buffer) { }

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
            public void onError(int error)
            {
                Enqueue(() =>
                {
                    IsListening = false;
                    CurrentState = VoiceState.Idle;
                    StateChanged?.Invoke(CurrentState);
                    HandleError(error);
                });
            }

            [UnityEngine.Scripting.Preserve]
            public void onResults(AndroidJavaObject results)
            {
                string text = null;

                try
                {
                    var matches = results.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
                    if (matches != null)
                    {
                        int size = matches.Call<int>("size");
                        if (size > 0)
                        {
                            text = matches.Call<string>("get", 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[VoiceInput] onResults parsing error: {ex.Message}");
                }

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
            public void onPartialResults(AndroidJavaObject partialResults) { }

            [UnityEngine.Scripting.Preserve]
            public void onEvent(int eventType, AndroidJavaObject @params) { }
        }
#endif
    }
}
