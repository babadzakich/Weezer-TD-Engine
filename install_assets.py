import os
import shutil
from pathlib import Path

def setup_game_directories():
    """Создает структуру папок игры в AppData"""
    app_data = os.getenv('APPDATA')
    if not app_data:
        print("Ошибка: Не удалось найти путь к AppData")
        return None
    
    root_dir = Path(app_data) / "WeezerTowerDefence"
    sub_dirs = [
        "common",
        "Saves",
        "DLLs",
        "DLLs/towers",
        "DLLs/enemies",
        "DLLs/damageDealers",
        "Levels",
        "Editor",
        "Editor/Assets",
        "Editor/Maps"
    ]
    
    print(f"\n[1] Проверка структуры папок в: {root_dir}")
    
    for sub in sub_dirs:
        path = root_dir / sub
        if not path.exists():
            path.mkdir(parents=True, exist_ok=True)
            print(f"    + Создана папка: {sub}")
        
    return root_dir

def copy_local_assets(target_root):
    """Копирует файлы из локальной папки 'game_assets' в AppData"""
    local_assets_dir = Path(__file__).parent / "game_assets"
    
    if not local_assets_dir.exists():
        print(f"\n[!] Локальная папка '{local_assets_dir.name}' не найдена. Пропускаю копирование.")
        return

    print(f"\n[2] Копирование ресурсов и DLL из '{local_assets_dir}'...")
    
    files_copied = 0
    # Проходим по всем файлам в local_assets_dir (включая подпапки)
    for item in local_assets_dir.rglob('*'):
        if item.is_file():
            # Относительный путь (например, 'common/EngineFont.xnb' или 'DLLs/towers/MyTower.dll')
            relative_path = item.relative_to(local_assets_dir)
            target_path = target_root / relative_path
            
            # Гарантируем наличие целевой папки
            target_path.parent.mkdir(parents=True, exist_ok=True)
            
            # Копируем файл (shutil.copy2 сохраняет метаданные)
            try:
                shutil.copy2(item, target_path)
                # Выделяем DLL в логах для наглядности
                ext = item.suffix.lower()
                status = f"    [DLL]  {relative_path}" if ext == '.dll' else f"    [FILE] {relative_path}"
                print(status)
                files_copied += 1
            except Exception as e:
                print(f"    [!] Ошибка копирования {relative_path}: {e}")

    if files_copied == 0:
        print("    В папке 'game_assets' не найдено файлов для копирования.")
    else:
        print(f"\n--- Успешно скопировано файлов: {files_copied} ---")

def main():
    print("=== Weezer Tower Defence: Установщик ресурсов ===")
    game_path = setup_game_directories()
    if game_path:
        copy_local_assets(game_path)
        print(f"\nГотово! Путь к игровым данным: {game_path}")

if __name__ == "__main__":
    main()
