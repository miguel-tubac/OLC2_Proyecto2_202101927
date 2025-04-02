import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import NavBar from './components/NavBar';
import TablaErrores from './components/TablaErrores'; // Importar el componente de TablaErrores
import TablaSimbolos from './components/TablaSimbolos';
import ASTViewer from './components/ASTViewer';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<NavBar />} />
        <Route path="/errores" element={<TablaErrores />} />
        <Route path="/simbolos" element={<TablaSimbolos />} />
        <Route path="/Compile/ast" element={<ASTViewer />} />
      </Routes>
    </Router>
  );
}

export default App;
