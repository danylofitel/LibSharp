# Code style

Generally speaking, there is no right or wrong when it comes to specific styles. For the most part they are a matter of personal preferences.
That said, we are going to enfore a specific coding style that we agree upon, and there are at least 2 good reasons for doing that:

* The team spends zero time arguing about their subjective preferences when IDE makes those decisions
* Having the style enforced by IDE means that all code is consistently formatted. It makes it easier to read for team members as they are used to it, and it's very easy to change from one style to another

Here are the language specific styles.

## C\#

The general coding style is based on the standard C# style.

### Naming Conventions

* Interfaces start with "I" and use CamelCap, e.g. IMyInterface
* Classes use CamelCap, e.g. MyClass
* Functions use CamelCap, e.g. MyMethods
* Properties use CamelCap, e.g. MyProperty
* Constants use CamelCap, e.g. MyConstant
* Static members start with "s_" prefix and use camelCase, e.g. s_myStaticMember
* Class members start with "m_" prefix and use camelCase, e.g. m_myClassMember
* Local variables use camelCase, e.g. myVariable
* Function parameters use camelCase, e.g. myParameter

### Namespaces

* Namespaces correspond to the folder structure

### Files

* Each file contains only one class
* The name of the file matches the name of the class it contains

### Using Statements

* Usings are placed on top of the file outside of namespace
* System usings are placed first
* Usings are sorted alphabetically
* Usings should be fully qualified

### Access Modifiers

* All entities should have explicit access modifiers
* Everything starts as private by default and is exposed only if necessary
* Protected and default access modifiers should be avoided
* Constructors, methods and properties may be public, members must be private

### Class Structure

Elements within a class are ordered as follows:

* Constructors
* Public properties
* Public methods
* Private properties
* Private methods
* Private members
* Private static members
* Private constants

### Layout

* Spaces are used for indentation
* Class members are separated by a single line
* Lines should be short enough to fit on most screens
* Classes should not exceed ~300 lines for simplicity/readability
* Constructor and function parameters should either be placed on the same line as the declaration or each on its own line

### Keywords and Features

* .this qualifier should not be used
* var keyword should not be used
* out and ref parameters should not be used in new functions
* Members should be declared as readonly by default

### Other

* IDisposable objects must be disposed
* Constructors and public methods must validate input parameters
* String operations should specify comparison types, Ordinal or OrdinalIgnoreCase by default
