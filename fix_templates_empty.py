#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Исправление шаблонов кода - оставляем только пустые шаблоны
"""

import json

def fix_code_templates_empty():
    """Исправляет шаблоны кода, оставляя пустой Main для студентов"""
    
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # Пустые шаблоны для каждого типа задания
    templates = {
        "chapter-1-section-1-code-1": {  # Main с выводом
            "starter": """using System;

public class Solution
{
    public static void Main()
    {
        // Ваш код здесь
    }
}"""
        },
        "chapter-1-section-2-code-1": {  # Main с переменной
            "starter": """using System;

public class Solution
{
    public static void Main()
    {
        // Объявите переменную x и установите значение 42
        // Console.WriteLine(x);
    }
}"""
        },
        "chapter-1-section-3-code-1": {  # Метод с возвращаемым значением
            "starter": """using System;

public class Solution
{
    public static double CalculateAverage(int a, int b)
    {
        // Верните среднее арифметическое (a + b) / 2.0
    }
    
    public static void Main()
    {
        string[] args = System.Console.In.ReadToEnd().Split();
        int a = int.Parse(args[0]);
        int b = int.Parse(args[1]);
        Console.WriteLine(CalculateAverage(a, b));
    }
}"""
        },
        "chapter-2-section-1-code-1": {  # Метод Add
            "starter": """using System;

public class Solution
{
    public static void Add(int a, int b)
    {
        // Выведите сумму a и b
    }
    
    public static void Main()
    {
        string[] args = System.Console.In.ReadToEnd().Split();
        int a = int.Parse(args[0]);
        int b = int.Parse(args[1]);
        Add(a, b);
    }
}"""
        },
        "chapter-2-section-2-code-1": {  # Класс Person
            "starter": """using System;

public class Person
{
    public string Name { get; set; }
    
    public string GetInfo()
    {
        // Верните строку в формате: "{name} is a person"
    }
}

public class Solution
{
    public static void Main()
    {
        string name = System.Console.In.ReadLine();
        var person = new Person { Name = name };
        Console.WriteLine(person.GetInfo());
    }
}"""
        },
        "chapter-2-section-3-code-1": {  # Rectangle с конструктором
            "starter": """using System;

public class Rectangle
{
    private double width;
    private double height;
    
    public Rectangle(double w, double h)
    {
        // Инициализируйте width и height
    }
    
    public double Area()
    {
        // Верните площадь: width * height
    }
}

public class Solution
{
    public static void Main()
    {
        string[] args = System.Console.In.ReadToEnd().Split();
        double w = double.Parse(args[0]);
        double h = double.Parse(args[1]);
        var rect = new Rectangle(w, h);
        Console.WriteLine(rect.Area());
    }
}"""
        },
        "chapter-2-section-4-code-1": {  # Пространство имён
            "starter": """using System;
using System.Collections;

public class Solution
{
    public static void Main()
    {
        // Используйте ArrayList или другой класс из System.Collections
        // Выведите "Program executed successfully"
    }
}"""
        },
        "chapter-2-section-5-code-1": {  # BankAccount
            "starter": """using System;

public class BankAccount
{
    private decimal balance;
    
    public BankAccount(decimal initialBalance)
    {
        // Инициализируйте balance
    }
    
    public decimal GetBalance()
    {
        // Верните баланс
    }
}

public class Solution
{
    public static void Main()
    {
        decimal initial = decimal.Parse(System.Console.In.ReadLine());
        var account = new BankAccount(initial);
        Console.WriteLine(account.GetBalance());
    }
}"""
        },
        "chapter-2-section-6-code-1": {  # Point struct
            "starter": """using System;

public struct Point
{
    public double X;
    public double Y;
    
    public double Distance()
    {
        // Верните расстояние от начала координат: √(X² + Y²)
        // Используйте System.Math.Sqrt
    }
}

public class Solution
{
    public static void Main()
    {
        string[] args = System.Console.In.ReadToEnd().Split();
        double x = double.Parse(args[0]);
        double y = double.Parse(args[1]);
        var point = new Point { X = x, Y = y };
        Console.WriteLine(point.Distance());
    }
}"""
        },
        "chapter-2-section-7-code-1": {  # Value vs Reference types
            "starter": """using System;

public class Solution
{
    public static void Main()
    {
        // Создайте переменную типа значения (int)
        // Создайте ссылочный тип (string)
        // Продемонстрируйте их поведение при присваивании
        // Выведите "Value type and reference type behavior demonstrated"
    }
}"""
        }
    }
    
    # Обновляем все код-задания в JSON
    for chapter in data['chapters']:
        for section in chapter['sections']:
            for page in section['pages']:
                for task in page.get('tasks', []):
                    if task.get('type') == 'code':
                        task_id = task.get('id')
                        
                        # Ищем соответствующий шаблон
                        for section_id, template_data in templates.items():
                            if task_id == f"{section_id}":
                                task['starterCode'] = template_data['starter']
                                print(f"✓ Обновлен: {task_id}")
                                break
    
    # Сохраняем обновленный файл
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    
    print("\n✅ Все шаблоны кода обновлены!")
    print("✅ Метод Main теперь пуст с подсказками")
    print("✅ Студенты должны заполнить код сами")

if __name__ == '__main__':
    fix_code_templates_empty()
