#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Генератор практических заданий на код для каждого раздела курса C#
"""

import json

# Практические задания по кодированию для каждого раздела
code_tasks = {
    "chapter-1-section-1": {  # Общая структура программы
        "id": "chapter-1-section-1-code-1",
        "order": 5,
        "title": "Практика: Вывод сообщения",
        "prompt": "Напишите программу, которая выводит строку 'Hello, World!' в консоль.",
        "starter": "public static void Main()\n{\n    // Ваш код здесь\n}",
        "entry_point": {"class": "Solution", "method": "Main"},
        "tests": [
            {
                "input": "",
                "expected": "Hello, World!",
                "visibility": "visible"
            }
        ]
    },
    "chapter-1-section-2": {  # Переменные и константы
        "id": "chapter-1-section-2-code-1",
        "order": 4,
        "title": "Практика: Объявление переменных",
        "prompt": "Объявите переменную целого типа x, установите её значение на 42 и выведите в консоль.",
        "starter": "public static void Main()\n{\n    // Объявите переменную и установите значение\n    // Console.WriteLine(x);\n}",
        "entry_point": {"class": "Solution", "method": "Main"},
        "tests": [
            {
                "input": "",
                "expected": "42",
                "visibility": "visible"
            }
        ]
    },
    "chapter-1-section-3": {  # Типы данных
        "id": "chapter-1-section-3-code-1",
        "order": 4,
        "title": "Практика: Работа с типами данных",
        "prompt": "Напишите метод, который принимает два целых числа и возвращает их среднее арифметическое (как double).",
        "starter": "public static double CalculateAverage(int a, int b)\n{\n    // Ваш код здесь\n}",
        "entry_point": {"class": "Solution", "method": "CalculateAverage"},
        "tests": [
            {
                "input": "10 20",
                "expected": "15",
                "visibility": "visible"
            },
            {
                "input": "5 15",
                "expected": "10",
                "visibility": "visible"
            },
            {
                "input": "100 200",
                "expected": "150",
                "visibility": "hidden"
            }
        ]
    },
    "chapter-2-section-1": {  # Классы и методы
        "id": "chapter-2-section-1-code-1",
        "order": 3,
        "title": "Практика: Вызов метода",
        "prompt": "Напишите метод Add, который складывает два числа и выводит результат в консоль.",
        "starter": "public static void Add(int a, int b)\n{\n    // Ваш код здесь\n}",
        "entry_point": {"class": "Solution", "method": "Add"},
        "tests": [
            {
                "input": "5 3",
                "expected": "8",
                "visibility": "visible"
            },
            {
                "input": "10 20",
                "expected": "30",
                "visibility": "hidden"
            }
        ]
    },
    "chapter-2-section-2": {  # Классы и объекты
        "id": "chapter-2-section-2-code-1",
        "order": 5,
        "title": "Практика: Создание класса",
        "prompt": "Создайте класс Person с полем Name и методом GetInfo(), который возвращает строку '{name} is a person'.",
        "starter": "public class Person\n{\n    public string Name { get; set; }\n    \n    public string GetInfo()\n    {\n        // Ваш код здесь\n    }\n}",
        "entry_point": {"class": "Person", "method": "GetInfo"},
        "tests": [
            {
                "input": "Alice",
                "expected": "Alice is a person",
                "visibility": "visible"
            },
            {
                "input": "Bob",
                "expected": "Bob is a person",
                "visibility": "hidden"
            }
        ]
    },
    "chapter-2-section-3": {  # Конструкторы
        "id": "chapter-2-section-3-code-1",
        "order": 3,
        "title": "Практика: Конструктор класса",
        "prompt": "Создайте класс Rectangle с конструктором, принимающим ширину и высоту, и методом Area(), возвращающим площадь.",
        "starter": "public class Rectangle\n{\n    private double width;\n    private double height;\n    \n    public Rectangle(double w, double h)\n    {\n        // Инициализируйте поля\n    }\n    \n    public double Area()\n    {\n        // Верните площадь\n    }\n}",
        "entry_point": {"class": "Rectangle", "method": "Area"},
        "tests": [
            {
                "input": "5 4",
                "expected": "20",
                "visibility": "visible"
            },
            {
                "input": "10 3",
                "expected": "30",
                "visibility": "hidden"
            }
        ]
    },
    "chapter-2-section-4": {  # Пространство имен
        "id": "chapter-2-section-4-code-1",
        "order": 2,
        "title": "Практика: Использование пространств имён",
        "prompt": "Используя System.Collections, напишите код, который работает с коллекциями из данного пространства имён.",
        "starter": "using System.Collections;\n\npublic static void Main()\n{\n    // Используйте ArrayList или другой класс из пространства имён\n}",
        "entry_point": {"class": "Solution", "method": "Main"},
        "tests": [
            {
                "input": "",
                "expected": "Program executed successfully",
                "visibility": "visible"
            }
        ]
    },
    "chapter-2-section-5": {  # Модификаторы доступа
        "id": "chapter-2-section-5-code-1",
        "order": 3,
        "title": "Практика: Модификаторы доступа",
        "prompt": "Создайте класс BankAccount с приватными полями для баланса и публичным методом GetBalance().",
        "starter": "public class BankAccount\n{\n    private decimal balance;\n    \n    public BankAccount(decimal initialBalance)\n    {\n        balance = initialBalance;\n    }\n    \n    public decimal GetBalance()\n    {\n        // Вернуть баланс\n    }\n}",
        "entry_point": {"class": "BankAccount", "method": "GetBalance"},
        "tests": [
            {
                "input": "1000",
                "expected": "1000",
                "visibility": "visible"
            }
        ]
    },
    "chapter-2-section-6": {  # Структуры
        "id": "chapter-2-section-6-code-1",
        "order": 3,
        "title": "Практика: Структура (Struct)",
        "prompt": "Создайте структуру Point с полями X и Y, и методом Distance(), вычисляющим расстояние от начала координат.",
        "starter": "public struct Point\n{\n    public double X;\n    public double Y;\n    \n    public double Distance()\n    {\n        // Вычислите √(X² + Y²)\n    }\n}",
        "entry_point": {"class": "Point", "method": "Distance"},
        "tests": [
            {
                "input": "3 4",
                "expected": "5",
                "visibility": "visible"
            },
            {
                "input": "0 0",
                "expected": "0",
                "visibility": "hidden"
            }
        ]
    },
    "chapter-2-section-7": {  # Типы значений и ссылочные типы
        "id": "chapter-2-section-7-code-1",
        "order": 3,
        "title": "Практика: Типы значений vs ссылочные типы",
        "prompt": "Напишите код, демонстрирующий разницу между типами значений и ссылочными типами при присваивании.",
        "starter": "public static void Compare()\n{\n    // Создайте переменную типа значения и ссылочного типа\n    // и продемонстрируйте их поведение\n}",
        "entry_point": {"class": "Solution", "method": "Compare"},
        "tests": [
            {
                "input": "",
                "expected": "Value type and reference type behavior demonstrated",
                "visibility": "visible"
            }
        ]
    }
}

def add_code_tasks():
    """Добавляет практические задания на код в JSON файл"""
    
    # Читаем исходный файл
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # Обновляем каждую главу и секцию
    for chapter in data['chapters']:
        for section in chapter['sections']:
            section_id = section['id']
            
            if section_id not in code_tasks:
                continue
            
            task_data = code_tasks[section_id]
            
            # Ищем quiz страницу, после неё добавим страницу с практическим заданием
            for i, page in enumerate(section['pages']):
                if page.get('kind') == 'quiz':
                    # Создаём новую страницу с практическим заданием
                    code_page = {
                        "id": f"{section_id}-code-page-1",
                        "order": task_data['order'],
                        "title": task_data['title'],
                        "estimatedTimeMinutes": 15,
                        "tasks": [
                            {
                                "id": task_data['id'],
                                "type": "code",
                                "title": task_data['title'],
                                "prompt": task_data['prompt'],
                                "starterCode": task_data['starter'],
                                "entryPoint": {
                                    "class": task_data['entry_point']['class'],
                                    "method": task_data['entry_point']['method'],
                                    "signature": ""
                                },
                                "constraints": {
                                    "timeLimitMs": 5000,
                                    "memoryLimitMb": 128,
                                    "disallow": []
                                },
                                "scoring": {
                                    "points": 5,
                                    "partial": False
                                },
                                "tests": [
                                    {
                                        "id": f"{task_data['id']}-test-{j}",
                                        "visibility": test['visibility'],
                                        "input": test['input'],
                                        "expectedOutput": test['expected']
                                    }
                                    for j, test in enumerate(task_data['tests'], 1)
                                ],
                                "hints": [
                                    "Начните с написания основного кода.",
                                    "Убедитесь, что ваш код обрабатывает все тестовые случаи.",
                                    "Проверьте, правильно ли выглядит вывод."
                                ],
                                "options": [],
                                "solution": {
                                    "correctOptionIds": [],
                                    "explanation": "Выполняется автоматическим тестированием."
                                }
                            }
                        ],
                        "resources": []
                    }
                    
                    # Вставляем после quiz страницы
                    section['pages'].insert(i + 1, code_page)
                    break
    
    # Записываем обновленный файл
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    
    print("✓ Практические задания на код добавлены!")
    print(f"✓ Добавлено {len(code_tasks)} заданий по разделам")

if __name__ == '__main__':
    add_code_tasks()
