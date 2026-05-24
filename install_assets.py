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
    
    print(f"--- Настройка игрового окружения в: {root_dir} ---")
    
    for sub in sub_dirs:
        path = root_dir / sub
        path.mkdir(parents=True, exist_ok=True)
        print(f"Создана папка: {path}")
        
    return root_dir

def copy_local_assets(target_root):
    """Копирует файлы из локальной папки 'game_assets' в AppData"""
    # Папка с ассетами рядом со скриптом
    local_assets_dir = Path(__file__).parent / "game_assets"
    
    if not local_assets_dir.exists():
        print(f"\n[!] Локальная папка '{local_assets_dir.name}' не найдена рядом со скриптом.")
        print(f"    Пропускаю копирование файлов.")
        return

    print(f"\n--- Копирование файлов из {local_assets_dir} ---")
    
    # Рекурсивное копирование содержимого
    # Мы проходим по всем файлам в local_assets_dir
    for item in local_assets_dir.rglob('*'):
        if item.is_file():
            # Вычисляем относительный путь, чтобы сохранить структуру подпапок
            relative_path = item.relative_to(local_assets_dir)
            target_path = target_root / relative_path
            
            # Создаем родительские папки если их нет
            target_path.parent.mkdir(parents=True, exist_ok=True)
            
            # Копируем файл
            try:
                shutil.copy2(item, target_path)
                print(f"Скопирован: {relative_path}")
            except Exception as e:
                print(f"Ошибка при копировании {relative_path}: {e}")

def main():
    game_path = setup_game_directories()
    if game_path:
        copy_local_assets(game_path)
        print("\n--- Установка завершена! ---")
        print(f"Игровые файлы находятся в: {game_path}")

if __name__ == "__main__":
    main()
