## Resolucion
Este proyecto es es Proyecto numero 1 de Organización de Lenguajes y Compiladores 2, en el mismo se aplica el patron de Visitor con la aplicacion de la herramiente de ANTLR. En la carpeta Enuncuado se encuentran las instrucciones del sistema. Enla parte de la Docuemntacion se encuantran los manuales de sistema.

## Development

Primero estar en la carpeta "backend" y ahi ejecutar los comandos

1. Generar la gramática en el proyecto:
```sh
rm -rf analyzer/*
```

```sh
antlr4 -Dlanguage=CSharp -o analyzer -package analyzer -visitor -no-listener ./grammars/*.g4
```


2. Para levantar el backend (estando en la carpeta backend):

```sh
dotnet watch run
```

3. Para levantar el Frontend (estando en la carpeta client)
```sh
npm run start
```


## Aplicacion
El programa resuelve el problema de un intérprete para el lenguaje de programación GoLight, un lenguaje diseñado con una sintaxis inspirada en Go, pero adaptado para explorar conceptos fundamentales de compiladores.
