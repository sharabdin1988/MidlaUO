package net.midla.uo;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;
import android.util.Log;
import java.util.ArrayList;

public class VoiceRecognizerHelper {
    private static final String TAG = "MidlaVoice";
    private static SpeechRecognizer speechRecognizer;
    private static Handler mainHandler;
    private static volatile boolean isHolding = false;
    private static final StringBuilder accumulatedText = new StringBuilder();
    private static Intent savedIntent;

    public interface Callback {
        void onReady();
        void onBeginning();
        void onEndOfSpeech();
        void onResults(String text);
        void onError(int errorCode, String errorMessage);
    }

    private static Handler getHandler() {
        if (mainHandler == null) {
            mainHandler = new Handler(Looper.getMainLooper());
        }
        return mainHandler;
    }

    public static boolean isAvailable(Context context) {
        try {
            return SpeechRecognizer.isRecognitionAvailable(context);
        } catch (Throwable t) {
            Log.w(TAG, "isAvailable check failed: " + t.getMessage());
            return false;
        }
    }

    public static void start(final Activity activity, final String language, final Callback callback) {
        start(activity, language, false, callback);
    }

    public static void start(final Activity activity, final String language, final boolean holdMode, final Callback callback) {
        getHandler().post(new Runnable() {
            @Override
            public void run() {
                try {
                    isHolding = holdMode;
                    accumulatedText.setLength(0);

                    if (speechRecognizer != null) {
                        try {
                            speechRecognizer.destroy();
                        } catch (Throwable ignored) {}
                        speechRecognizer = null;
                    }

                    speechRecognizer = SpeechRecognizer.createSpeechRecognizer(activity);
                    if (speechRecognizer == null) {
                        if (callback != null) {
                            callback.onError(-1, "Служба распознавания речи недоступна");
                        }
                        return;
                    }

                    speechRecognizer.setRecognitionListener(new RecognitionListener() {
                        @Override
                        public void onReadyForSpeech(Bundle params) {
                            Log.d(TAG, "onReadyForSpeech");
                            if (callback != null) callback.onReady();
                        }

                        @Override
                        public void onBeginningOfSpeech() {
                            Log.d(TAG, "onBeginningOfSpeech");
                            if (callback != null) callback.onBeginning();
                        }

                        @Override
                        public void onRmsChanged(float rmsdB) {}

                        @Override
                        public void onBufferReceived(byte[] buffer) {}

                        @Override
                        public void onEndOfSpeech() {
                            Log.d(TAG, "onEndOfSpeech (isHolding=" + isHolding + ")");
                            if (!isHolding && callback != null) {
                                callback.onEndOfSpeech();
                            }
                        }

                        @Override
                        public void onError(int error) {
                            Log.w(TAG, "onError: " + error + " (isHolding=" + isHolding + ")");

                            // В режиме удержания: если пауза в речи или таймаут, пока кнопка зажата — перезапускаем и продолжаем слушать!
                            if (isHolding && (error == SpeechRecognizer.ERROR_SPEECH_TIMEOUT || error == SpeechRecognizer.ERROR_NO_MATCH)) {
                                Log.d(TAG, "Silence in hold mode, continuing listening...");
                                try {
                                    speechRecognizer.startListening(savedIntent);
                                } catch (Throwable t) {
                                    Log.w(TAG, "restart listening error: " + t.getMessage());
                                }
                                return;
                            }

                            // Если кнопку уже отпустили и есть накопленный текст — отдаем его игроку
                            if (!isHolding && accumulatedText.length() > 0 &&
                                (error == SpeechRecognizer.ERROR_SPEECH_TIMEOUT || error == SpeechRecognizer.ERROR_NO_MATCH)) {
                                String fullResult = accumulatedText.toString().trim();
                                accumulatedText.setLength(0);
                                if (callback != null) callback.onResults(fullResult);
                                return;
                            }

                            accumulatedText.setLength(0);
                            String msg = getErrorMessage(error);
                            if (callback != null) callback.onError(error, msg);
                        }

                        @Override
                        public void onResults(Bundle results) {
                            String bestResult = "";
                            if (results != null) {
                                ArrayList<String> matches = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
                                if (matches != null && !matches.isEmpty()) {
                                    bestResult = matches.get(0);
                                }
                            }
                            Log.d(TAG, "onResults: " + bestResult + " (isHolding=" + isHolding + ")");

                            if (isHolding) {
                                // Кнопка ещё зажата! Сохраняем распознанный фрагмент и продолжаем слушать
                                if (bestResult != null && !bestResult.trim().isEmpty()) {
                                    if (accumulatedText.length() > 0) {
                                        accumulatedText.append(" ");
                                    }
                                    accumulatedText.append(bestResult.trim());
                                }
                                try {
                                    speechRecognizer.startListening(savedIntent);
                                } catch (Throwable t) {
                                    Log.w(TAG, "startListening in onResults error: " + t.getMessage());
                                }
                            } else {
                                // Кнопка отпущена (или одиночный тап) — формируем финальный результат
                                String fullResult;
                                if (accumulatedText.length() > 0) {
                                    if (bestResult != null && !bestResult.trim().isEmpty()) {
                                        accumulatedText.append(" ").append(bestResult.trim());
                                    }
                                    fullResult = accumulatedText.toString().trim();
                                } else {
                                    fullResult = bestResult != null ? bestResult.trim() : "";
                                }
                                accumulatedText.setLength(0);
                                Log.d(TAG, "Delivering final onResults: " + fullResult);
                                if (callback != null) callback.onResults(fullResult);
                            }
                        }

                        @Override
                        public void onPartialResults(Bundle partialResults) {}

                        @Override
                        public void onEvent(int eventType, Bundle params) {}
                    });

                    Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                    intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
                    intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, (language != null && !language.isEmpty()) ? language : "ru-RU");
                    intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 3);
                    intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, false);

                    if (holdMode) {
                        // В режиме удержания (Push-to-Talk): длинные таймауты (слушать пока держишь кнопку)
                        intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_MINIMUM_LENGTH_MILLIS, 60000L);
                        intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, 60000L);
                        intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, 60000L);
                    }

                    savedIntent = intent;

                    Log.d(TAG, "Starting speech recognition (holdMode=" + holdMode + ")...");
                    speechRecognizer.startListening(intent);
                } catch (Throwable t) {
                    Log.e(TAG, "startListening error", t);
                    if (callback != null) {
                        callback.onError(-2, "Ошибка старта: " + t.getMessage());
                    }
                }
            }
        });
    }

    public static void stop() {
        getHandler().post(new Runnable() {
            @Override
            public void run() {
                isHolding = false;
                if (speechRecognizer != null) {
                    try {
                        speechRecognizer.stopListening();
                    } catch (Throwable t) {
                        Log.w(TAG, "stopListening error: " + t.getMessage());
                    }
                }
            }
        });
    }

    public static void cancel() {
        getHandler().post(new Runnable() {
            @Override
            public void run() {
                isHolding = false;
                accumulatedText.setLength(0);
                if (speechRecognizer != null) {
                    try {
                        speechRecognizer.cancel();
                        speechRecognizer.destroy();
                    } catch (Throwable t) {
                        Log.w(TAG, "cancel error: " + t.getMessage());
                    }
                    speechRecognizer = null;
                }
            }
        });
    }

    private static String getErrorMessage(int error) {
        switch (error) {
            case SpeechRecognizer.ERROR_AUDIO: return "Ошибка аудио (микрофон занят)";
            case SpeechRecognizer.ERROR_CLIENT: return "Ошибка приложения";
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS: return "Нет разрешения на запись аудио";
            case SpeechRecognizer.ERROR_NETWORK: return "Ошибка сети";
            case SpeechRecognizer.ERROR_NETWORK_TIMEOUT: return "Таймаут соединения с сетью";
            case SpeechRecognizer.ERROR_NO_MATCH: return "Речь не распознана";
            case SpeechRecognizer.ERROR_RECOGNIZER_BUSY: return "Служба распознавания занята";
            case SpeechRecognizer.ERROR_SERVER: return "Ошибка сервера Google";
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT: return "Время ожидания речи истекло";
            default: return "Ошибка #" + error;
        }
    }
}
