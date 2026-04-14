import { createTheme, defaultThemes } from "react-data-table-component";

export const individuality = {
  text: {
    primary: "var(--bs-dark)",
    secondary: "var(--bs-secondary)",
  },
  context: {
    background: "#268bd2",
    text: "var(--bs-light)",
  },
  divider: {
    default: defaultThemes.default.divider.default,
  },
  action: {
    button: "var(--bs-light)",
    hover: "rgba(0,0,0,.08)",
    disabled: "rgba(0,0,0,.12)",
  },
};

export const themeIndividuality = createTheme(
  "individuality",
  individuality,
  "dark",
);

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

export const compactGrid = {
  header: {
    style: {
      minHeight: "60px",
      background: "var(--solusersa-head)",
      color: "var(--bs-light)",
      fontWeight: "bold",
      fontSize: "30px",
      textAlign: "center",
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
      },
    },
  },
  rows: {
    stripedStyle: {
      backgroundColor: "var(--solusersa)",
      color: "black",
    },
    style: {
      background: "white",
    },
  },
  cells: {
    style: {
      "&:not(:last-of-type)": {
        borderRightStyle: "solid",
        borderRightWidth: "1px",
        borderRightColor: defaultThemes.default.divider.default,
      },
    },
  },
  pagination: {
    style: {
      border: "none",
      background: "var(--bs-container)",
      color: "black",
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
