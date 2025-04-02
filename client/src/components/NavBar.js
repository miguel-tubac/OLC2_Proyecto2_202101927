import React, { useRef, useState, useEffect } from 'react';
import AreadeTexto1 from './AreadeTexto1';
import AreadeTexto2 from './AreadeTexto2';
import { useNavigate } from 'react-router-dom';
import ASTViewer from './ASTViewer';


const NavBar = ({ onGenerateAST }) => {
  const fileInputRef = useRef(null);
  const [fileContent, setFileContent] = useState('');
  const [responseContent, setResponseContent] = useState('');
  const [errorList, setErrorList] = useState([]);
  const [simbolosList, setSimbolosList] = useState([]);
  const [astData, setAstData] = useState(null);

  const navigate = useNavigate();

  // Efecto para cargar el estado desde localStorage
  useEffect(() => {
    const savedFileContent = localStorage.getItem('fileContent');
    const savedResponseContent = localStorage.getItem('responseContent');
    const savedErrorList = localStorage.getItem('errorList');
    const savedSimbolList = localStorage.getItem('simbolosList');

    if (savedFileContent) {
      setFileContent(savedFileContent);
    }
    if (savedResponseContent) {
      setResponseContent(savedResponseContent);
    }
    if (savedErrorList) {
      setErrorList(JSON.parse(savedErrorList));
    }
    if (savedSimbolList) {
      setSimbolosList(JSON.parse(savedSimbolList));
    }
  }, []);

  // Efecto para guardar el estado en localStorage
  useEffect(() => {
    localStorage.setItem('fileContent', fileContent);
    localStorage.setItem('responseContent', responseContent);
    localStorage.setItem('errorList', JSON.stringify(errorList));
    localStorage.setItem('simbolosList', JSON.stringify(simbolosList));
  }, [fileContent, responseContent, errorList, simbolosList]);

  const handleFileButtonClick = () => {
    fileInputRef.current.click();
  };

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file && file.name.endsWith('.glt')) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target.result;
        setFileContent(content);
      };
      reader.readAsText(file);
    } else {
      console.log('Por favor selecciona un archivo con extensión .glt');
    }
  };

  const handleExecuteButtonClick = () => {
    sendDataToBackend(fileContent);
  };

  const handleErrorsButtonClick = () => {
    navigate('/errores', { state: { errores: errorList } });
  };

  const handleSimbolsButtonClick = () => {
    navigate('/simbolos', { state: { simbolos: simbolosList } });
  };

  const sendDataToBackend = (data) => {
    const backendUrl = 'http://localhost:5003/Compile';

    fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ Code: data }),
    })
      .then((response) => response.json())
      .then((data) => {
        console.log("Respuesta propuest: ", data);
        let resultado = data.consola;
        let errores = data.tablaError;
        let simbolos = data.tablaSimbolos;
        //console.log('Respuesta del backend:', resultado);
        //console.log('Respuesta de Error:', errores);
        console.log('Respuesta de simbolos:', simbolos);
        setResponseContent(resultado);
        setErrorList(errores);
        setSimbolosList(simbolos);
      })
      .catch((error) => console.error('Error al enviar datos al backend:', error));
  };

  const handleGenerateAST = async () => {
    try {
      const response = await fetch("http://localhost:5003/Compile/ast", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ code: fileContent }),
      });
  
      if (!response.ok) throw new Error("Error en la generación del AST");
  
      // Aquí obtenemos la respuesta como texto, que debería ser el contenido SVG
      const svgContent = await response.text();
  
      // Guardamos el contenido SVG en el estado
      setAstData(svgContent); 
    } catch (error) {
      console.error("Error al generar el AST:", error);
    }
  };
  
  


  return (
    <div>
      <nav
        style={{
          marginBottom: '20px',
          backgroundImage: 'linear-gradient(to right, black, purple)',
          padding: '10px',
        }}
      >
        <button style={{ marginRight: '20px' }} onClick={handleFileButtonClick}>
          Archivo
        </button>
        <input
          ref={fileInputRef}
          type="file"
          accept=".glt"
          style={{ display: 'none' }}
          onChange={handleFileChange}
        />
        <button style={{ marginRight: '20px' }} onClick={handleExecuteButtonClick}>
          Ejecutar
        </button>
        <button style={{ marginRight: '20px' }} onClick={handleErrorsButtonClick}>
          Tabla Errores
        </button>
        <button style={{ marginRight: '20px' }} onClick={handleSimbolsButtonClick}>
          Tabla Simbolos
        </button>
        <button style={{ marginRight: '20px' }} onClick={handleGenerateAST}>
          Generar AST
        </button>
      </nav>

      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
        <AreadeTexto1 fileContent={fileContent} setFileContent={setFileContent} />
        <AreadeTexto2 responseContent={responseContent} />
      </div>
        {astData && <ASTViewer astData={astData} onClose={() => setAstData(null)} />}
    </div>
  );
};

export default NavBar;
