## Code Blocks
delimeted by '`{`' and '`}`' or '`:`' and '`end`'
## Comments
```ruby
# Coments delimented by single `#`
```
## Methods
```python
def method (param1, param2):
	pass
end
```
## Lists
```python
list = ['this', 'list', 'holds', 10, 'items', 'and', 'is', 'indexed', 'at', 0]
print(list[4])  # prints 'items'
list.Add(9)
list.Insert(1, 5)
```
## Variables
_LevelScript features dynamic but strong typing, similar to python_
```ruby
foo = 5		# int
bar = "10"	# string
print (foo + bar)	# results in error
print (foo + int(bar))	# prints `15`
print (str(foo) + bar)	# prints `510`
```
## Conditional
```lua
if this:
	print("this")
else if that:
	print (that)
else:
	print("none of the aboave")
end
```
## Loops
```ruby
for i in 0..10:
	print(i)
end
```
```ruby
while (condition):
	print("condition is true")
end
```
## Casting / Parsing
_Again similar to python, casting/parsing is done by calling the type's conversion method_
```python
number = int("5")
string = str(5)
```
## Operators
Type | Symbols
--- | ---
__Maths__ | `+ - * / %`
__Boolean__ | `and or zor nor` or `&& || !| !&`  
__Comparisons__ | `== < > <= >=`  
__Assign__ | `= += -+ /= *=`  
__Assign__ | `= += -+ /= *=`
__Index__ | `[]`
__Invoke__ | `()`
__Acess__ | `.`
__Range__ | `..`
