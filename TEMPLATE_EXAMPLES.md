# Примеры исправленных шаблонов кода

## Задание 1.1: Вывод сообщения

```csharp
using System;

public class Solution
{
    public static void Main()
    {
        Console.WriteLine("Hello, World!");
    }
}
```

**Тест:** "" → "Hello, World!"

---

## Задание 1.2: Объявление переменной

```csharp
using System;

public class Solution
{
    public static void Main()
    {
        int x = 42;
        Console.WriteLine(x);
    }
}
```

**Тест:** "" → "42"

---

## Задание 1.3: Расчет среднего арифметического

```csharp
using System;

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
}
```

**Тесты:**
- "10 20" → "15"
- "5 15" → "10"
- "100 200" → "150" (скрытый)

---

## Задание 2.1: Вызов метода сложения

```csharp
using System;

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
}
```

**Тесты:**
- "5 3" → "8"
- "10 20" → "30" (скрытый)

---

## Задание 2.2: Создание класса Person

```csharp
using System;

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
}
```

**Тесты:**
- "Alice" → "Alice is a person"
- "Bob" → "Bob is a person" (скрытый)

---

## Задание 2.3: Конструктор Rectangle

```csharp
using System;

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
}
```

**Тесты:**
- "5 4" → "20"
- "10 3" → "30" (скрытый)

---

## Задание 2.4: Пространство имён (System.Collections)

```csharp
using System;
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
}
```

**Тест:** "" → "Program executed successfully"

---

## Задание 2.5: Модификаторы доступа (BankAccount)

```csharp
using System;

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
}
```

**Тест:** "1000" → "1000"

---

## Задание 2.6: Структура Point

```csharp
using System;

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
}
```

**Тесты:**
- "3 4" → "5"
- "0 0" → "0" (скрытый)

---

## Задание 2.7: Типы значений vs ссылочные типы

```csharp
using System;

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
}
```

**Тест:** "" → "Value type and reference type behavior demonstrated"

---

## Ключевые особенности исправленных шаблонов

### 1. **Using инструкции**
Каждый шаблон содержит нужные импорты:
- `using System;` - для Console
- `using System.Collections;` - для ArrayList

### 2. **Класс Solution (точка входа)**
Каждый шаблон имеет класс `Solution` с методом `Main()`, который:
- Читает входные данные
- Парсит их нужными типами
- Вызывает пользовательский код

### 3. **Обработка входных данных**
```csharp
// Одна строка
string name = System.Console.In.ReadLine();

// Несколько значений (разделены пробелами)
string[] args = System.Console.In.ReadToEnd().Split();
int a = int.Parse(args[0]);
int b = int.Parse(args[1]);
```

### 4. **Правильный вывод**
```csharp
// Всегда используется Console.WriteLine
Console.WriteLine(result);
```

### 5. **Полная структура класса**
```csharp
public class MyClass
{
    private int field;
    
    public MyClass(int value)
    {
        field = value;
    }
    
    public int Method()
    {
        return field;
    }
}
```

---

## Проверка

✅ Все 10 шаблонов обновлены  
✅ Проект компилируется без ошибок  
✅ Каждый шаблон имеет нужные импорты  
✅ Каждый метод находится внутри класса  
✅ Обработка входных данных корректна  

---

**Готовый код для студентов! 🎉**
