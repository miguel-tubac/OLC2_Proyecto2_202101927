using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using analyzer;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;

namespace api.Controllers
{
    [Route("compile")]
    [ApiController] // Agrega esto para evitar ambigüedades
    public class Compile : Controller
    {
        private readonly ILogger<Compile> _logger;

        public Compile(ILogger<Compile> logger)
        {
            _logger = logger;
        }


        public void GenerarArchivo(string contenido)
        {
            // Ruta donde se guardará el archivo
            string ruta = "/home/miguel/Descargas/Compi2/Laboratorio/Proyecto_2/Salida_ARM64/pruebas.s";
            
            try
            {
                // Escribir el contenido en el archivo
                System.IO.File.WriteAllText(ruta, contenido);
                Console.WriteLine($"Archivo creado exitosamente en: {ruta}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al crear el archivo: " + ex.Message);
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("error")]
        public IActionResult Error()
        {
            return View("Error!");
        }

        public class CompileRequest
        {
            [Required]
            public required string Code { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CompileRequest request)
        {
            //En esta lista lamacenara los obejetos de tipo error
            List<ErrorEntry> unionEror = new List<ErrorEntry>();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Compiling code: {0}", request.Code);
            
            var consola2 = "";//Se almacenaran los errores y se agregaran al final a la consola del frontend
            var inputStream = new AntlrInputStream(request.Code);
            var lexer = new LanguageLexer(inputStream);

            //Esta es la parte del errores Lexicos
            var lexicalErrors = new LexicalErrorListener();
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(lexicalErrors);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new LanguageParser(tokenStream);

            //Esto es para los errores sintacticos
            var syntaxErrors = new SyntaxErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(syntaxErrors);


            // **Lista de errores semánticos**
            var visitor = new InterpreterVisitor();
            var compiler = new CompilerVisitor();

            try{
                var tree = parser.program();

                // **Primera pasada:** Buscar declaraciones y el cuerpo del main
                var searchVisitor = new SearchVisitor();
                searchVisitor.Visit(tree);

                // **Segunda pasada:** Ejecutar en orden
                //var visitor = new CompileVisitor();

                foreach (var decl in searchVisitor.Declaraciones)
                {
                    visitor.Visit(decl);
                }

                foreach (var func in searchVisitor.Funciones)
                {
                    visitor.Visit(func);
                }

                // Ejecutar el `main`
                foreach (var stmt in searchVisitor.MainBody)
                {
                    visitor.Visit(stmt);
                }

                //Aca es la nueva parte del transpilador
                //compiler.Visit(tree);
                foreach (var decl in searchVisitor.Declaraciones)
                {
                    compiler.Visit(decl);
                }

                foreach (var func in searchVisitor.Funciones)
                {
                    compiler.Visit(func);
                }

                // Ejecutar el `main`
                foreach (var stmt in searchVisitor.MainBody)
                {
                    compiler.Visit(stmt);
                }

                //Esto era para la primera fase
                //consola2 += visitor.output;
            }catch(ParseCanceledException ex){
                consola2 += ex.Message;
                //Se concatenan los errores semanticos y lexicos
                unionEror.AddRange(syntaxErrors.Errors);
                unionEror.AddRange(lexicalErrors.Errors);
            }catch(SemanticError ex){
                consola2 += ex.Message;
                //Se agrega el error semantico manualmente
                unionEror.Add(new ErrorEntry(ex.message, ex.token.Line, ex.token.Column, "Error Semantico"));
            }catch(ReturnException ex){
                consola2 += ex.Message;
            }catch(BreakException ex){
                consola2 += ex.Message;
            }catch(ContinueException ex){
                consola2 += ex.Message;
            }catch(Exception ex){
                consola2 += ex.Message;
            }

            //Aca se valida si ocurrio algun error
            if (consola2 != ""){
                //Aca se manda el resultado al frontend
                var response = new
                {
                    consola = consola2,
                    //consola = compiler.c.ToString(),
                    tablaError = unionEror,
                    tablaSimbolos = visitor.simbolos
                };
                return Ok(response);
            }else{
                //Aca se genera el archivo
                GenerarArchivo(compiler.c.ToString());
                //Aca se manda el resultado al frontend
                var response = new
                {
                    //consola = consola2,
                    consola = compiler.c.ToString(),
                    tablaError = unionEror,
                    tablaSimbolos = visitor.simbolos
                };
                return Ok(response);
            }

            //Fianliza
        }


        //Aca se generara una nueva peticion para el ast
        [HttpPost("ast")]
        public async Task<IActionResult> GetAst([FromBody] CompileRequest request)
        {
            if(!ModelState.IsValid){
                return BadRequest(ModelState);
            }

            string grammerPath = Path.Combine(Directory.GetCurrentDirectory(), "./grammars/Language.g4");

            var grammar = "";

            try{
                if(System.IO.File.Exists(grammerPath)){
                    grammar = await System.IO.File.ReadAllTextAsync(grammerPath);
                }
                else{
                    return BadRequest(new {error = "Grammar file no encontrada"});
                }
            }catch(System.Exception){
                return BadRequest(new {error = "Error leyendo el archivo de gramatica"});
            }

            //Esto es lo que se le envia a la api de Antlr
            var payload = new 
            {
                grammar,
                lexgrammar = "",
                input = request.Code,
                start = "program"
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var context = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                try{
                    HttpResponseMessage response = await client.PostAsync("http://lab.antlr.org/parse", context);
                    response.EnsureSuccessStatusCode();

                    string result = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(result);
                    var root = doc.RootElement;

                    if(root.TryGetProperty("result", out JsonElement resultElement) && 
                        resultElement.TryGetProperty("svgtree", out JsonElement svgtreeElement))
                    {
                        string svgtree = svgtreeElement.GetString() ?? string.Empty;
                        return Content(svgtree, "image/svg+xml");
                        // Modificación para enviar un JSON con el contenido de svgtree
                        //return Json(new { ast = svgtree });
                    }
                    return BadRequest(new {error = "svgtree no se encontro en la respuesta"});

                }catch(System.Exception){
                    return BadRequest(new {error = "Error al leer el retorno de la Api de Antlr"});
                }
            }

        } 
    }
}


