# Weezer Tower Defence Makefile

.PHONY: install run edit help

# Команда по умолчанию
help:
	@echo "Доступные команды:"
	@echo "  make install - Подготовить папки в AppData и скопировать ассеты из 'game_assets'"
	@echo "  make run     - Запустить основную игру Weezer Tower Defence"
	@echo "  make edit    - Запустить редактор уровней Weezer Tower Defence Editor"

# Установка окружения и ассетов
install:
	@echo "--- Настройка игрового окружения ---"
	python install_assets.py

# Запуск игры
run:
	@echo "--- Запуск Weezer Tower Defence ---"
	dotnet run --project Weezer-Tower-Defence/Weezer-Tower-Defence-Game.csproj

# Запуск редактора
edit:
	@echo "--- Запуск Weezer Tower Defence Editor ---"
	dotnet run --project Weezer-Tower-Defence-Editor/Weezer-Tower-Defence-Editor.csproj
