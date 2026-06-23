#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Генератор уникальных вопросов для каждого раздела курса C#
"""

import json

# Определяем специфичные вопросы для каждого раздела
section_questions = {
    "chapter-1-section-1": {  # Общая структура программы
        "quiz1": {
            "title": "Основные компоненты программы",
            "question": "Какой метод является точкой входа для консольного приложения?",
            "options": [
                {"text": "Метод Main", "correct": True, "feedback": "Верно: Main — это точка входа программы."},
                {"text": "Метод Begin", "correct": False, "feedback": "Неверно: такого метода нет в C#."},
                {"text": "Метод Start", "correct": False, "feedback": "Неверно: Start используется в других фреймворках."}
            ],
            "explanation": "В C# точка входа программы — это публичный статический метод Main."
        },
        "quiz2": {
            "title": "Структура кода",
            "question": "Какие утверждения верны о структуре программы на C#?",
            "options": [
                {"text": "Программа должна содержать класс Program с методом Main", "correct": True, "feedback": "Верно: это обязательная структура."},
                {"text": "Пространство имен System автоматически подключается", "correct": False, "feedback": "Неверно: нужно явно указать using System."},
                {"text": "Все инструкции должны быть заключены в класс", "correct": True, "feedback": "Верно: весь код должен быть организован в классы."},
                {"text": "Можно обойтись без класса и писать код прямо в файле", "correct": False, "feedback": "Неверно: в C# весь код должен быть внутри класса."}
            ],
            "explanation": "В C# код обязательно организуется в классы, а программа начинает выполнение с метода Main."
        }
    },
    "chapter-1-section-2": {  # Переменные и константы
        "quiz1": {
            "title": "Различие между переменной и константой",
            "question": "Какая главная разница между переменной и константой в C#?",
            "options": [
                {"text": "Константа не может изменяться после инициализации", "correct": True, "feedback": "Верно: констант неизменяемы."},
                {"text": "Переменная занимает больше памяти", "correct": False, "feedback": "Неверно: размер не отличается."},
                {"text": "Константа видна только внутри метода", "correct": False, "feedback": "Неверно: область видимости не связана с типом хранилища."}
            ],
            "explanation": "Главное отличие: константу можно установить только один раз, а переменную можно менять много раз."
        },
        "quiz2": {
            "title": "Объявление переменных и констант",
            "question": "Выберите верные утверждения о переменных и константах:",
            "options": [
                {"text": "Переменную можно объявить с ключевым словом const", "correct": False, "feedback": "Неверно: const используется только для констант."},
                {"text": "Константа требует явной инициализации при объявлении", "correct": True, "feedback": "Верно: const всегда должна быть инициализирована."},
                {"text": "Переменная может быть объявлена без инициализации", "correct": True, "feedback": "Верно: переменную можно инициализировать позже."},
                {"text": "const string name; — валидное объявление", "correct": False, "feedback": "Неверно: константа должна быть инициализирована."}
            ],
            "explanation": "Переменные гибче в использовании и могут быть инициализированы позже, константы должны быть установлены сразу."
        }
    },
    "chapter-1-section-3": {  # Типы данных
        "quiz1": {
            "title": "Числовые типы данных",
            "question": "Какой тип данных используется для целых чисел со знаком?",
            "options": [
                {"text": "int", "correct": True, "feedback": "Верно: int — основной тип для целых чисел."},
                {"text": "float", "correct": False, "feedback": "Неверно: float — для дробных чисел."},
                {"text": "bool", "correct": False, "feedback": "Неверно: bool — для логических значений."}
            ],
            "explanation": "int может хранить целые числа в диапазоне от -2,147,483,648 до 2,147,483,647."
        },
        "quiz2": {
            "title": "Типы данных и их применение",
            "question": "Какие утверждения верны о типах данных в C#?",
            "options": [
                {"text": "double используется для дробных чисел с высокой точностью", "correct": True, "feedback": "Верно: double обеспечивает точность до 15-17 знаков."},
                {"text": "string может содержать только буквы", "correct": False, "feedback": "Неверно: string может содержать любые символы."},
                {"text": "bool может содержать значения true или false", "correct": True, "feedback": "Верно: это единственные значения для bool."},
                {"text": "decimal предназначен для денежных операций", "correct": True, "feedback": "Верно: decimal идеален для финансовых расчётов."}
            ],
            "explanation": "Выбор типа данных важен для эффективности программы и точности вычислений."
        }
    },
    "chapter-2-section-1": {  # Классы и методы (Program Main)
        "quiz1": {
            "title": "Метод Main и точка входа",
            "question": "Почему метод Main объявляется как static?",
            "options": [
                {"text": "Чтобы его можно было вызвать без создания экземпляра класса", "correct": True, "feedback": "Верно: static методы принадлежат классу, а не объекту."},
                {"text": "Для экономии памяти", "correct": False, "feedback": "Неверно: это не влияет на память."},
                {"text": "Потому что это требование языка", "correct": True, "feedback": "Верно: .NET требует static Main как точку входа."}
            ],
            "explanation": "Main должен быть static, чтобы среда выполнения могла вызвать его без создания объекта программы."
        },
        "quiz2": {
            "title": "Параметры Main и void",
            "question": "Выберите верные утверждения о методе Main:",
            "options": [
                {"text": "Main может принимать параметр string[] args", "correct": True, "feedback": "Верно: это массив аргументов командной строки."},
                {"text": "Main всегда должен возвращать int", "correct": False, "feedback": "Неверно: Main может возвращать void или int."},
                {"text": "Main может быть приватным методом", "correct": False, "feedback": "Неверно: Main должен быть public."},
                {"text": "Из Main можно вызывать другие методы", "correct": True, "feedback": "Верно: Main — это просто метод, может вызывать другие методы."}
            ],
            "explanation": "Main — это главный метод, вызываемый при запуске программы."
        }
    },
    "chapter-2-section-2": {  # Классы и объекты
        "quiz1": {
            "title": "Классы и объекты",
            "question": "Что такое объект в отношении к классу?",
            "options": [
                {"text": "Экземпляр класса, созданный с помощью ключевого слова new", "correct": True, "feedback": "Верно: объект — это конкретный экземпляр класса."},
                {"text": "Синоним класса", "correct": False, "feedback": "Неверно: класс — это описание, объект — экземпляр."},
                {"text": "Часть класса, которая содержит только методы", "correct": False, "feedback": "Неверно: объект содержит и данные, и методы."}
            ],
            "explanation": "Класс — это шаблон, объект — конкретный экземпляр этого шаблона."
        },
        "quiz2": {
            "title": "Создание и использование объектов",
            "question": "Выберите верные утверждения об объектах:",
            "options": [
                {"text": "Объект создаётся с помощью оператора new", "correct": True, "feedback": "Верно: new выделяет память и инициализирует объект."},
                {"text": "Один класс может создать только один объект", "correct": False, "feedback": "Неверно: из класса можно создать множество объектов."},
                {"text": "Каждый объект имеет свои экземплярные переменные", "correct": True, "feedback": "Верно: каждый объект имеет независимые данные."},
                {"text": "Объект автоматически удаляется из памяти", "correct": True, "feedback": "Верно: сборщик мусора в .NET автоматически очищает память."}
            ],
            "explanation": "Объекты — основные единицы объектно-ориентированного программирования."
        }
    },
    "chapter-2-section-3": {  # Конструкторы
        "quiz1": {
            "title": "Назначение конструктора",
            "question": "Для чего предназначен конструктор класса?",
            "options": [
                {"text": "Для инициализации полей объекта при его создании", "correct": True, "feedback": "Верно: конструктор инициализирует объект."},
                {"text": "Для удаления объекта из памяти", "correct": False, "feedback": "Неверно: это делает деструктор или сборщик мусора."},
                {"text": "Для создания файлов на диске", "correct": False, "feedback": "Неверно: конструктор работает с памятью."}
            ],
            "explanation": "Конструктор вызывается автоматически когда создаётся новый объект класса."
        },
        "quiz2": {
            "title": "Конструкторы и их типы",
            "question": "Выберите верные утверждения о конструкторах:",
            "options": [
                {"text": "Конструктор по умолчанию не имеет параметров", "correct": True, "feedback": "Верно: default constructor создаётся автоматически."},
                {"text": "Класс может иметь несколько конструкторов с разными параметрами", "correct": True, "feedback": "Верно: это называется перегрузка конструктора."},
                {"text": "Конструктор обязательно должен возвращать значение", "correct": False, "feedback": "Неверно: конструктор ничего не возвращает."},
                {"text": "Имя конструктора совпадает с именем класса", "correct": True, "feedback": "Верно: конструктор всегда имеет имя класса."}
            ],
            "explanation": "Конструкторы позволяют установить начальное состояние объектов."
        }
    },
    "chapter-2-section-4": {  # Пространство имен
        "quiz1": {
            "title": "Пространства имён и организация кода",
            "question": "Какова главная цель использования пространств имён?",
            "options": [
                {"text": "Избежать конфликтов имён и организовать код по смыслу", "correct": True, "feedback": "Верно: namespace предотвращает коллизии имён."},
                {"text": "Ускорить компиляцию программы", "correct": False, "feedback": "Неверно: никак не влияет на скорость."},
                {"text": "Уменьшить размер памяти", "correct": False, "feedback": "Неверно: это не связано с памятью."}
            ],
            "explanation": "Пространства имён группируют связанный код и предотвращают конфликты имён классов."
        },
        "quiz2": {
            "title": "Using и импорт типов",
            "question": "Выберите верные утверждения о using и пространствах имён:",
            "options": [
                {"text": "using позволяет использовать типы без полного имени", "correct": True, "feedback": "Верно: используя using, можно писать Console вместо System.Console."},
                {"text": "Можно создавать вложенные пространства имён", "correct": True, "feedback": "Верно: например System.Collections.Generic."},
                {"text": "using System должна быть в двойных кавычках", "correct": False, "feedback": "Неверно: using не использует кавычки."},
                {"text": "Класс может принадлежать только одному пространству имён", "correct": True, "feedback": "Верно: класс определяется в одном namespace."}
            ],
            "explanation": "Пространства имён организуют код иерархически, улучшая читаемость и масштабируемость."
        }
    },
    "chapter-2-section-5": {  # Модификаторы доступа
        "quiz1": {
            "title": "Основные модификаторы доступа",
            "question": "Какой модификатор доступа разрешает доступ только из того же класса?",
            "options": [
                {"text": "private", "correct": True, "feedback": "Верно: private ограничивает доступ только классом."},
                {"text": "public", "correct": False, "feedback": "Неверно: public доступен везде."},
                {"text": "protected", "correct": False, "feedback": "Неверно: protected доступен и производным классам."}
            ],
            "explanation": "private — самый ограничивающий модификатор, используется для внутренних данных класса."
        },
        "quiz2": {
            "title": "Уровни доступа",
            "question": "Выберите верные утверждения о модификаторах доступа:",
            "options": [
                {"text": "public — доступен из любого места в коде", "correct": True, "feedback": "Верно: это самый открытый уровень."},
                {"text": "protected позволяет доступ производным классам", "correct": True, "feedback": "Верно: protected работает для наследников."},
                {"text": "internal ограничивает доступ только одним файлом", "correct": False, "feedback": "Неверно: internal открыт для всей сборки."},
                {"text": "Если не указан модификатор, то по умолчанию private", "correct": True, "feedback": "Верно: по умолчанию члены приватны."}
            ],
            "explanation": "Модификаторы доступа контролируют видимость членов класса и обеспечивают инкапсуляцию."
        }
    },
    "chapter-2-section-6": {  # Структуры
        "quiz1": {
            "title": "Структуры в C#",
            "question": "Чем структура отличается от класса?",
            "options": [
                {"text": "Структура — это тип значения, класс — тип ссылки", "correct": True, "feedback": "Верно: struct — value type, class — reference type."},
                {"text": "Структура не может иметь методов", "correct": False, "feedback": "Неверно: структура может содержать методы."},
                {"text": "Скорость работы структур выше", "correct": True, "feedback": "Верно: структуры работают быстрее благодаря размещению в стеке."}
            ],
            "explanation": "Структуры хранятся в стеке, классы — в куче, что влияет на производительность и поведение."
        },
        "quiz2": {
            "title": "Структуры и их использование",
            "question": "Выберите верные утверждения о структурах:",
            "options": [
                {"text": "Структура может наследоваться от другой структуры", "correct": False, "feedback": "Неверно: struct не поддерживает наследование."},
                {"text": "Структура может реализовать интерфейс", "correct": True, "feedback": "Верно: struct может реализовывать интерфейсы."},
                {"text": "Переменная структуры содержит данные, а не ссылку", "correct": True, "feedback": "Верно: это ключевая разница от классов."},
                {"text": "При передаче структуры в метод создаётся копия", "correct": True, "feedback": "Верно: структуры передаются по значению."}
            ],
            "explanation": "Структуры полезны для простых типов данных, требующих высокой производительности."
        }
    },
    "chapter-2-section-7": {  # Типы значений и ссылочные типы
        "quiz1": {
            "title": "Типы значений и ссылочные типы",
            "question": "Где в памяти хранятся типы значений в C#?",
            "options": [
                {"text": "В стеке", "correct": True, "feedback": "Верно: value types хранятся в стеке."},
                {"text": "В куче", "correct": False, "feedback": "Неверно: в куче хранятся reference types."},
                {"text": "В специальной памяти для типов", "correct": False, "feedback": "Неверно: это не так."}
            ],
            "explanation": "int, double, struct и enum — типы значений, они хранятся в стеке и передаются по значению."
        },
        "quiz2": {
            "title": "Поведение типов при передаче",
            "question": "Выберите верные утверждения:",
            "options": [
                {"text": "При передаче типа значения копируется всё значение", "correct": True, "feedback": "Верно: происходит полное копирование."},
                {"text": "При передаче ссылочного типа копируется ссылка", "correct": True, "feedback": "Верно: передаётся только ссылка на объект."},
                {"text": "string — это ссылочный тип", "correct": True, "feedback": "Верно: string относится к reference types."},
                {"text": "null можно присвоить типу значения", "correct": False, "feedback": "Неверно: value types не могут быть null (кроме nullable<T>)."}
            ],
            "explanation": "Разница между типами значений и ссылочными заметна при передаче параметров и присваивании."
        }
    },
}

def update_json_content():
    """Обновляет вопросы в JSON файле"""
    
    # Читаем исходный файл
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # Обновляем каждую главу и секцию
    for chapter in data['chapters']:
        for section in chapter['sections']:
            section_id = section['id']
            
            if section_id not in section_questions:
                continue
            
            questions_data = section_questions[section_id]
            
            # Ищем quiz страницу
            for page in section['pages']:
                if page.get('kind') == 'quiz':
                    tasks = page['tasks']
                    
                    # Обновляем первый вопрос (single choice)
                    if len(tasks) > 0 and tasks[0]['id'].endswith('quiz-1-single'):
                        q1_data = questions_data['quiz1']
                        tasks[0]['title'] = q1_data['title']
                        tasks[0]['question'] = q1_data['question']
                        tasks[0]['solution']['explanation'] = q1_data['explanation']
                        
                        for i, option in enumerate(tasks[0]['options']):
                            if i < len(q1_data['options']):
                                option['text'] = q1_data['options'][i]['text']
                                option['isCorrect'] = q1_data['options'][i]['correct']
                                option['feedback'] = q1_data['options'][i]['feedback']
                    
                    # Обновляем второй вопрос (multiple choice)
                    if len(tasks) > 1 and tasks[1]['id'].endswith('quiz-2-multiple'):
                        q2_data = questions_data['quiz2']
                        tasks[1]['title'] = q2_data['title']
                        tasks[1]['question'] = q2_data['question']
                        tasks[1]['solution']['explanation'] = q2_data['explanation']
                        
                        for i, option in enumerate(tasks[1]['options']):
                            if i < len(q2_data['options']):
                                option['text'] = q2_data['options'][i]['text']
                                option['isCorrect'] = q2_data['options'][i]['correct']
                                option['feedback'] = q2_data['options'][i]['feedback']
                        
                        # Обновляем правильные ответы
                        correct_ids = [opt['id'] for opt in tasks[1]['options'] if opt['isCorrect']]
                        tasks[1]['solution']['correctOptionIds'] = correct_ids
    
    # Записываем обновленный файл
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    
    print("✓ JSON файл успешно обновлён!")
    print("✓ Все вопросы теперь соответствуют темам разделов!")

if __name__ == '__main__':
    update_json_content()
