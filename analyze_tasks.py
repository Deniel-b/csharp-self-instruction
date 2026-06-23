#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Статистика заданий в курсе C#
"""

import json

def analyze_tasks():
    """Анализирует все задания в курсе"""
    
    with open('e:\\CodeProjects\\orders\\diplomSidorova\\csharp-self-instruction\\src\\content.v2.json', 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    stats = {
        'quiz_tasks': 0,
        'code_tasks': 0,
        'total_tests': 0,
        'visible_tests': 0,
        'hidden_tests': 0,
        'sections': {},
        'tasks_by_type': {}
    }
    
    for chapter in data['chapters']:
        for section in chapter['sections']:
            section_id = section['id']
            section_name = section['title']
            stats['sections'][section_id] = {
                'title': section_name,
                'quiz': 0,
                'code': 0,
                'tests': 0
            }
            
            for page in section['pages']:
                for task in page.get('tasks', []):
                    task_type = task.get('type', 'unknown')
                    
                    if task_type not in stats['tasks_by_type']:
                        stats['tasks_by_type'][task_type] = 0
                    stats['tasks_by_type'][task_type] += 1
                    
                    if task_type == 'quiz':
                        stats['quiz_tasks'] += 1
                        stats['sections'][section_id]['quiz'] += 1
                    elif task_type == 'code':
                        stats['code_tasks'] += 1
                        stats['sections'][section_id]['code'] += 1
                    
                    # Подсчитываем тесты
                    tests = task.get('tests', [])
                    for test in tests:
                        stats['total_tests'] += 1
                        stats['sections'][section_id]['tests'] += 1
                        
                        visibility = test.get('visibility', 'visible')
                        if visibility == 'visible':
                            stats['visible_tests'] += 1
                        else:
                            stats['hidden_tests'] += 1
    
    return stats

def print_stats(stats):
    """Выводит статистику в красивом формате"""
    
    print("\n" + "="*80)
    print("СТАТИСТИКА ЗАДАНИЙ КУРСА C#")
    print("="*80)
    
    print(f"\n📊 ОБЩАЯ СТАТИСТИКА:")
    print(f"   • Quiz задания:        {stats['quiz_tasks']:2d}")
    print(f"   • Code задания:        {stats['code_tasks']:2d}")
    print(f"   • Всего тестов:        {stats['total_tests']:2d}")
    print(f"     └─ Видимые:         {stats['visible_tests']:2d}")
    print(f"     └─ Скрытые:         {stats['hidden_tests']:2d}")
    
    print(f"\n📋 ПО ТИПАМ ЗАДАНИЙ:")
    for task_type, count in sorted(stats['tasks_by_type'].items()):
        print(f"   • {task_type.capitalize():12} - {count} заданий")
    
    print(f"\n📚 ПО РАЗДЕЛАМ:")
    print(f"\n{'Раздел':<40} {'Quiz':<7} {'Code':<7} {'Тесты':<7}")
    print("-" * 65)
    
    for section_id in sorted(stats['sections'].keys()):
        section = stats['sections'][section_id]
        print(f"{section['title']:<40} {section['quiz']:<7} {section['code']:<7} {section['tests']:<7}")
    
    print("\n" + "="*80)
    print(f"✅ ИТОГО: {stats['quiz_tasks'] + stats['code_tasks']} заданий, {stats['total_tests']} тестов")
    print("="*80 + "\n")

if __name__ == '__main__':
    stats = analyze_tasks()
    print_stats(stats)
