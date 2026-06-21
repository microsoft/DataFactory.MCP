# Copy Job Patterns

## Copy Job Definition Structure

Copy Job definitions specify source-to-destination data movement configuration:

- **Source**: Connection reference, table/file selection, query or filter
- **Destination**: Target connection, table name, write mode (replace/append)
- **Column Mappings**: Source→destination column assignments with type conversions
- **CDC Configuration**: Change data capture settings for incremental replication

## CDC vs Batch Mode

| Mode | When | How |
|------|------|-----|
| **Batch** | Full data replacement on each run | Default — copies entire source |
| **CDC** | Incremental replication | Tracks changes via watermark or change tracking |

### CDC Requirements
- Source must support change tracking (SQL Server, Azure SQL)
- Upsert key columns must be specified for merge behavior
- First run does a full copy; subsequent runs copy only changes

## Column Mapping Patterns

### Automatic Mapping
Columns are matched by name. Works when source and destination schemas align.

### Explicit Mapping
Define specific source→destination column assignments:
- Rename columns during copy
- Select subset of source columns
- Apply type conversions

## Write Modes

| Mode | Behavior |
|------|----------|
| **Replace** | Drop and recreate destination data each run |
| **Append** | Add new rows to existing data |
| **Upsert** | Insert new rows, update existing (requires key columns) |

## When to Use Copy Job vs Dataflow

- **Copy Job**: Straight source→dest movement, minimal or no transforms
- **Dataflow Gen2**: Complex M transforms, multi-source joins, aggregations, custom logic
- **Pipeline**: Orchestrate multiple Copy Jobs or Dataflows with dependencies
