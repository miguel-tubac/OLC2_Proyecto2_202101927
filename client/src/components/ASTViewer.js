import React from 'react';

const ASTViewer = ({ astData, onClose }) => {
  return (
    <div 
      style={{
        position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
        backgroundColor: 'rgba(0,0,0,0.7)', display: 'flex',
        justifyContent: 'center', alignItems: 'center'
      }}
    >
      <div style={{ backgroundColor: 'white', padding: '20px', borderRadius: '10px', maxWidth: '80%', maxHeight: '80%', overflowY: 'auto' }}>
        <h2>Árbol de Sintaxis Abstracta (AST)</h2>
        
        {/* Aquí insertamos el SVG */}
        <div 
          dangerouslySetInnerHTML={{ __html: astData }} 
          style={{ maxWidth: '100%', maxHeight: '80vh', overflow: 'auto' }}
        />

        <button onClick={onClose} style={{ marginTop: '10px' }}>Cerrar</button>
      </div>
    </div>
  );
};

export default ASTViewer;
