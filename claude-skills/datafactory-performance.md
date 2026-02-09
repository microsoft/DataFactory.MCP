# Data Factory Performance & Query Organization

## Query Performance

### Handling Timeouts

When a query times out, chunk by a key column (e.g., date, ID, region) to process data in smaller batches. 

Example approach:
1. Identify a suitable partitioning column (dates work well)
2. Run the query for smaller ranges (e.g., one month at a time)
3. Aggregate results as needed

### Filter Early

Apply filters as early as possible in your query. This enables query folding (pushing filters to the data source) and reduces data volume for subsequent steps.

### Expensive Operations Last

Operations like sorting require reading all data before returning results. Place these at the end. Streaming operations (filters, column selection) can go first since they process data incrementally.

### Work on a Subset During Development

When building queries, use "Keep First Rows" to limit data during development. Remove this step once your transformations are complete.

---

## Connectors & Data Types

### Choose the Right Connector

Use native connectors (e.g., SQL Server connector) over generic ones (ODBC/OLEDB). Native connectors offer better query folding and optimized performance.

### Set Correct Data Types

Always define data types explicitly, especially for unstructured sources (CSV, TXT). Type-specific filters and transformations only appear when columns have correct types assigned.

---

## Query Organization

### Modular Queries

Split large queries into smaller referenced queries. Use "Extract Previous" to break a query at a specific step. This improves readability and reusability.

### Use Groups

Organize queries into groups (folders) in the Queries pane. Drag and drop to reorganize.

### Document Steps

Rename steps and add descriptions in the Applied Steps pane. Future you will thank present you.

---

## Parameters & Functions

### Use Parameters

Store reusable values (server names, file paths, filter values) as parameters. Update once, apply everywhere.

### Create Reusable Functions

When applying the same transformations to multiple queries, create a custom function. Right-click a query → "Create Function".

---

## Future-Proofing

### Handle Dynamic Data

- Use "Remove bottom rows" for footers (not row filters)
- Use "Choose columns" to select specific columns (survives new columns being added)
- Use "Unpivot only selected columns" when column count varies

### Handle Errors Gracefully

If type conversions cause errors, consider removing error rows or replacing errors with defaults.
