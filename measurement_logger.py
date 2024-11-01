from datetime import datetime
import csv
import os
from typing import Tuple, Union
from pathlib import Path

def append_measurement(
    filepath: Union[str, Path],
    measurement: Tuple[Union[datetime, str], float, str],
    reset: bool
) -> None:
    """
    Appends a measurement record to a CSV file.
    
    Args:
        filepath: Path to the CSV file. If a relative path is provided, it will be relative
                 to the current working directory. Use an absolute path to ensure
                 consistent behavior regardless of where the script is run from.
        measurement: Tuple containing (timestamp, value, unit)
            timestamp can be either a datetime object or an ISO format string
            value should be a float
            unit should be a string
    
    Raises:
        ValueError: If measurement data is invalid
        IOError: If file operations fail or directory doesn't exist
    
    Notes:
        - If only a filename is provided (no path), the file will be created in the
          current working directory (the directory from which the script is run)
        - The function will create the file if it doesn't exist, but it won't create
          directories - the directory path must already exist
    """
    # Convert to Path object for better path handling
    filepath = Path(filepath)
    
    # Check if directory exists
    if not filepath.parent.exists():
        raise IOError(
            f"Directory {filepath.parent.absolute()} does not exist. "
            f"Current working directory is: {Path.cwd()}"
        )
    
    # Validate measurement data
    timestamp, value, unit = measurement
    if not isinstance(value, (int, float)):
        raise ValueError("Value must be a number")
    if not isinstance(unit, str):
        raise ValueError("Unit must be a string")
    
    # Convert timestamp to ISO format if it's a datetime object
    if isinstance(timestamp, datetime):
        timestamp = timestamp.isoformat()
    
    # Check if file exists and needs headers
    file_exists = filepath.exists()
    
    try:
        with open(filepath, 'a', newline='') as f:
            writer = csv.writer(f)
            
            # Write headers if new file
            if not file_exists:
                writer.writerow(['Timestamp', 'Value', 'Unit'])
            
            # Write the measurement
            writer.writerow([timestamp, value, unit])
            
    except IOError as e:
        raise IOError(f"Failed to write to file {filepath}: {str(e)}")