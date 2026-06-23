#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Исправление шаблонов кода для всех заданий
"""

import json

def fix_code_templates():
    """Исправляет шаблоны кода с правильной структурой класса"""
    
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # Правильные шаблоны для каждого типа задания
    templates = {
        "chapter-1-section-1-code-1": {  # Main с выводом
            "starter": """using System;

public class Solution
{
    public static void Main()
    {
        Console.WriteLine("Hello, World!");
    }
}"""
        },
        "chapter-1-section-2-code-1": {  # Main с переменной
            "starter": """using System;

public class Solution
{
    public static void Main()
    {
        int x = 42;
        Console.WriteLine(x);
    }
}"""
        },
        "chapter-1-section-3-code-1": {  # Метод с возвращаемым значением
            "starter": """using System;

public class Solution
{
    public static double CalculateAverage(int a, int b)
    {
        return (a + b) / 2.0;
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
        Console.WriteLine(a + b);
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
        return $"{Name} is a person";
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
        width = w;
        height = h;
    }
    
    public double Area()
    {
        return width * height;
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
        ArrayList list = new ArrayList();
        list.Add("item1");
        list.Add("item2");
        Console.WriteLine("Program executed successfully");
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
        balance = initialBalance;
    }
    
    public decimal GetBalance()
    {
        return balance;
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
        return System.Math.Sqrt(X * X + Y * Y);
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
        // Тип значения (struct)
        int a = 10;
        int b = a;
        b = 20;
        // a == 10, b == 20 (независимые копии)
        
        // Ссылочный тип (class)
        string s1 = "hello";
        string s2 = s1;
        // s1 и s2 указывают на одну строку
        
        Console.WriteLine("Value type and reference type behavior demonstrated");
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
    
    print("\n✅ Все шаблоны кода исправлены!")
    print("✅ Добавлены импорты")
    print("✅ Добавлены классы-обертки")
    print("✅ Методы Main теперь внутри класса")

if __name__ == '__main__':
    fix_code_templates()
