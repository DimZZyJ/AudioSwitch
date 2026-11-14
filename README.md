# AudioSwitch

WPF-приложение (MVVM, .NET 9) для быстрого переключения избранных аудиоустройств
ввода и вывода глобальными горячими клавишами. Основное окно сворачивается в трей,
переключение работает даже когда окно скрыто.

## Возможности

- Отдельные панели для устройств вывода (динамики) и ввода (микрофоны).
- Отметка устройств звёздочкой — они попадают в «избранное» и переключаются по кругу.
- Две настраиваемые глобальные горячие клавиши (по умолчанию `Ctrl+Alt+F9` — вывод,
  `Ctrl+Alt+F10` — ввод). Клавиша задаётся кликом по полю и нажатием комбинации;
  `Delete` очищает, `Esc` отменяет ввод.
- Смена устройства по умолчанию выполняется через Core Audio (`IPolicyConfig.SetDefaultEndpoint`)
  сразу для всех трёх ролей: Console, Multimedia, Communications.
- Иконка в трее: контекстное меню с текущими устройствами, быстрым переключением и выходом;
  двойной клик открывает окно. Кнопка «Свернуть в трей» и закрытие окна крестиком
  прячут приложение в трей (выход — только через меню трея или кнопку «Выход»).
- Настройки сохраняются в `%APPDATA%\AudioSwitch\settings.json`; список устройств
  автоматически обновляется каждые 3 секунды и при фокусе окна.
- Один экземпляр приложения (Mutex).

## Архитектура (MVVM)

| Слой | Файлы | Назначение |
| --- | --- | --- |
| Models | `AudioDevice`, `DeviceFlow`, `Hotkey`, `AppSettings` | Модели без зависимостей от UI |
| Services | `CoreAudioInterop`, `AudioDeviceService`, `SettingsService`, `HotkeyService`, `TrayIconService` | COM-interop Core Audio, JSON-настройки, RegisterHotKey в message-only окне, NotifyIcon |
| ViewModels | `ObservableObject`, `RelayCommand`, `DeviceItemViewModel`, `MainViewModel` | Состояние и команды; не знают о конкретные контролы |
| Views | `AudioPanel`, `HotkeyBox` + `MainWindow` | Только XAML и минимальный code-behind (трюк с WindowChrome/треем) |
| App | `App.xaml(.cs)` | Composition root: создаёт сервисы, ViewModel, окно, трей, регистрирует клавиши |

`MainViewModel` работает с устройствами через `IAudioDeviceService` и настройками через
`ISettingsService`, поэтому слой представления легко заменяем.

## Сборка и запуск

```powershell
dotnet build AudioSwitch.sln
dotnet run --project AudioSwitch
```

Требования: Windows 10/11, .NET 9 SDK. Иконки (`Assets/app.ico`, `Assets/logo.png`)
уже включены в проект.

## Сборка инсталятора

```powershell
.\build-installer.ps1
```

Скрипт публикует self-contained сборку `win-x64` (пользователю не нужен
установленный .NET) и компилирует установщик Inno Setup 6 из `installer\AudioSwitch.iss`.
Результат — `dist\AudioSwitch-<версия>-x64-setup.exe`; версия берётся из `<Version>` в csproj.
Реквизиты (версия, архитектура) прокидываются в `.iss` через `/D`-определения.

Требования: Inno Setup 6 — `winget install --id JRSoftware.InnoSetup`
(скрипт находит `ISCC.exe` по реестру и типовым путям, включая per-user установку).

Особенности установщика:

- Установка в профиль без UAC (`{autopf}` → `AppData\Local\Programs\AudioSwitch`),
  повышение через диалог/`/ALLUSERS` опционально.
- Рус/англ языки, ярлык в меню Пуск (+ опционально на рабочем столе), запись
  «Программы и компоненты», завершение запущенного экземпляра из папки установки
  перед копированием файлов, запуск по завершении мастера.
- При удалении чистит значение `AudioSwitch` в `HKCU\...\Run` (только если оно
  указывает на удаляемую папку) и предлагает удалить `%APPDATA%\AudioSwitch`
  с настройками.
- Флаг `-FrameworkDependent` — облегчённый установщик под уже установленный
  .NET 9 Desktop Runtime; `-Version`, `-Architecture`, `-Configuration` — параметры сборки.

`build-installer.ps1` и `installer\AudioSwitch.iss` должны оставаться в кодировке
UTF-8 **с BOM** (иначе PowerShell 5.1 и ISCC портят кириллицу).

