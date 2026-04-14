import { createTheme, defaultThemes } from "react-data-table-component";

createTheme('solusersa-theme', {
  text: {
    primary: 'var(--bs-dark)',       
    secondary: 'var(--bs-dark)',     
  },
  background: {
    default: 'var(--bs-white)',      
  },
  context: {
    background: 'var(--solusersa-head)', 
    text: 'var(--bs-white)',
  },
  divider: {
    default: 'var(--bs-gray-300)',
  },
  button: {
    default: 'var(--bs-dark)',
    hover: 'var(--bs-gray-300)',
    focus: 'var(--bs-gray-300)',
    disabled: 'var(--bs-gray-300)',
  },
  sortFocus: {
    default: 'var(--bs-dark)',
  },
}, 'default');

export const customStyles = {
  header: {
    style: {
      minHeight: "60px",
      background: "var(--solusersa-head)",
      color: "var(--bs-light)",
      fontWeight: "bold",
      fontSize: "30px",
      justifyContent: "center",
    },
  },
  subHeader: {
    style: {
      background: "var(--solusersa-head)",
      color: "var(--bs-light)",
    },
  },
  headRow: {
    style: {
      borderTopStyle: "solid",
      background: "var(--solusersa-head)",
      color: "var(--bs-light)",
      borderTopWidth: "1px",
      borderTopColor: defaultThemes.default.divider.default,
    },
  },
  headCells: {
    style: {
      "&:not(:last-of-type)": {
        borderRightStyle: "solid",
        borderRightWidth: "1px",
        borderRightColor: defaultThemes.default.divider.default,
        background: "var(--solusersa-head)",
      },
    },
  },
  cells: {
    style: {
      "&:not(:last-of-type)": {
        borderRightStyle: "solid",
        borderRightWidth: "1px",
        minHeight: "80px",
        borderRightColor: defaultThemes.default.divider.default,
        color: "var(--bs-light)",
      },
    },
  },
  rows: {
    stripedStyle: {
      backgroundColor: "white",
      color: "black",
    },
    style: {
      background: "var(--solusersa)",
    },
    highlightOnHoverStyle: {
      backgroundColor: "var(--solusersa-hover)",
      color: "var(--bs-light)",
      outline: "1px solid var(--bs-light)",
      outlineOffset: "-1px",
    },
  },
  pagination: {
    style: {
      border: "none",
      background: "var(--bs-container)",
      color: "var(--bs-light)",
    },
  },
  noData: {
    style: {
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      background: "var(--solusersa)",
    },
  },
  progress: {
    style: {
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      background: "var(--solusersa)",
    },
  },
};
