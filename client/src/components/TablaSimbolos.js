import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom'; // Importa useLocation para obtener los errores

const TablaSimbolos = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const simbolos = location.state?.simbolos || []; // Obtener los errores de la navegación

  return (
    <div>
      <h2 style={{ color: 'white' }}>Tabla de Simbolos</h2>
      {simbolos.length > 0 ? (
        <table style={{ width: '100%', color: 'white', backgroundColor: 'rgba(0, 0, 0, 0.5)', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ border: '1px solid white', color: 'white' }}>#</th> {/* Encabezado para el contador */}
              <th style={{ border: '1px solid white', color: 'white' }}>ID</th>
              <th style={{ border: '1px solid white', color: 'white' }}>Tipo símbolo</th>
              <th style={{ border: '1px solid white', color: 'white' }}>Tipo dato</th>
              <th style={{ border: '1px solid white', color: 'white' }}>Ámbito</th>
              <th style={{ border: '1px solid white', color: 'white' }}>Línea</th>
              <th style={{ border: '1px solid white', color: 'white' }}>Columna</th>
            </tr>
          </thead>
          <tbody>
            {simbolos.map((simbolo, index) => (
              <tr key={index}>
                <td style={{ border: '1px solid white', color: 'white' }}>{index + 1}</td> {/* Contador */}
                <td style={{ border: '1px solid white' }}>{simbolo.id}</td>
                <td style={{ border: '1px solid white' }}>{simbolo.tipo_simbol}</td>
                <td style={{ border: '1px solid white' }}>{simbolo.tipo_dato}</td>
                <td style={{ border: '1px solid white' }}>{simbolo.ambito}</td>
                <td style={{ border: '1px solid white' }}>{simbolo.linea}</td>
                <td style={{ border: '1px solid white' }}>{simbolo.columna}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p style={{ color: 'white' }}>No hay simbolos</p>
      )}

      <button onClick={() => navigate('/')}>Regresar al Menú Principal</button> {/* Botón para regresar */}
    </div>
  );
};

export default TablaSimbolos;
