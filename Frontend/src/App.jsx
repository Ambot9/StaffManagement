import { useEffect, useMemo, useState } from 'react'
import './App.css'

const emptyForm = {
  fullName: '',
  birthday: '',
  gender: '1',
}

const emptySearch = {
  staffId: '',
  gender: '',
  birthdayFrom: '',
  birthdayTo: '',
}

function App() {
  const [staffs, setStaffs] = useState([])
  const [search, setSearch] = useState(emptySearch)
  const [form, setForm] = useState(emptyForm)
  const [editingStaffId, setEditingStaffId] = useState(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const isEditing = Boolean(editingStaffId)

  useEffect(() => {
    async function loadInitialStaffs() {
      setIsLoading(true)

      try {
        const data = await apiGet('/api/Staffs')
        setStaffs(data)
      } catch (requestError) {
        setError(requestError.message)
      } finally {
        setIsLoading(false)
      }
    }

    loadInitialStaffs()
  }, [])

  const totalMale = useMemo(
    () => staffs.filter((staff) => staff.gender === 1).length,
    [staffs],
  )

  const totalFemale = useMemo(
    () => staffs.filter((staff) => staff.gender === 2).length,
    [staffs],
  )

  async function loadStaffs() {
    await runRequest(async () => {
      const data = await apiGet('/api/Staffs')
      setStaffs(data)
    })
  }

  async function searchStaffs(event) {
    event.preventDefault()

    await runRequest(async () => {
      const data = await apiGet(`/api/Staffs/search?${toQueryString(search)}`)
      setStaffs(data)
      setMessage(`Found ${data.length} staff record${data.length === 1 ? '' : 's'}.`)
    })
  }

  async function saveStaff(event) {
    event.preventDefault()

    const validationMessage = validateForm(form)

    if (validationMessage) {
      setError(validationMessage)
      return
    }

    const payload = {
      fullName: form.fullName.trim(),
      birthday: form.birthday,
      gender: Number(form.gender),
    }

    await runRequest(async () => {
      if (isEditing) {
        await apiSend(`/api/Staffs/${editingStaffId}`, 'PUT', payload)
        setMessage('Staff updated successfully.')
      } else {
        const createdStaff = await apiSend('/api/Staffs', 'POST', payload)
        setMessage(`Staff ${createdStaff.staffId} created successfully.`)
      }

      closeForm()
      await loadStaffs()
    })
  }

  async function deleteStaff(staffId) {
    const shouldDelete = window.confirm(`Delete staff ${staffId}?`)

    if (!shouldDelete) {
      return
    }

    await runRequest(async () => {
      await apiSend(`/api/Staffs/${staffId}`, 'DELETE')
      setMessage(`Staff ${staffId} deleted.`)
      await loadStaffs()
    })
  }

  function openCreateForm() {
    setForm(emptyForm)
    setEditingStaffId(null)
    setIsFormOpen(true)
    clearAlerts()
  }

  function openEditForm(staff) {
    setForm({
      fullName: staff.fullName,
      birthday: staff.birthday,
      gender: String(staff.gender),
    })
    setEditingStaffId(staff.staffId)
    setIsFormOpen(true)
    clearAlerts()
  }

  function closeForm() {
    setForm(emptyForm)
    setEditingStaffId(null)
    setIsFormOpen(false)
  }

  function resetSearch() {
    setSearch(emptySearch)
    loadStaffs()
  }

  function exportReport(type) {
    const path = type === 'excel' ? 'excel' : 'pdf'
    const queryString = toQueryString(search)
    const url = `/api/Staffs/export/${path}${queryString ? `?${queryString}` : ''}`

    window.open(url, '_blank', 'noopener,noreferrer')
  }

  async function runRequest(action) {
    setIsLoading(true)
    clearAlerts()

    try {
      await action()
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setIsLoading(false)
    }
  }

  function clearAlerts() {
    setError('')
    setMessage('')
  }

  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="Main navigation">
        <div className="brand-mark">SM</div>
        <nav>
          <a href="#staffs" className="nav-link active">Staffs</a>
          <a href="#search" className="nav-link">Search</a>
          <a href="#reports" className="nav-link">Reports</a>
        </nav>
      </aside>

      <section className="workspace">
        <header className="topbar">
          <div>
            <h1>Staff Management</h1>
            <p>Manage staff profiles, search by criteria, and export reports.</p>
          </div>
          <button type="button" className="primary-button" onClick={openCreateForm}>
            Add Staff
          </button>
        </header>

        <section className="stats-grid" aria-label="Staff summary">
          <SummaryStat label="Total staff" value={staffs.length} />
          <SummaryStat label="Male" value={totalMale} />
          <SummaryStat label="Female" value={totalFemale} />
        </section>

        <section className="panel" id="search">
          <div className="panel-header">
            <div>
              <h2>Advanced Search</h2>
              <p>Filter by staff ID, gender, and birthday range.</p>
            </div>
            <button type="button" className="ghost-button" onClick={resetSearch}>
              Reset
            </button>
          </div>

          <form className="search-form" onSubmit={searchStaffs}>
            <label>
              Staff ID
              <input
                value={search.staffId}
                onChange={(event) => setSearchField('staffId', event.target.value)}
                placeholder="STF00001"
              />
            </label>

            <label>
              Gender
              <select
                value={search.gender}
                onChange={(event) => setSearchField('gender', event.target.value)}
              >
                <option value="">All</option>
                <option value="1">Male</option>
                <option value="2">Female</option>
              </select>
            </label>

            <label>
              Birthday from
              <input
                type="date"
                value={search.birthdayFrom}
                onChange={(event) => setSearchField('birthdayFrom', event.target.value)}
              />
            </label>

            <label>
              Birthday to
              <input
                type="date"
                value={search.birthdayTo}
                onChange={(event) => setSearchField('birthdayTo', event.target.value)}
              />
            </label>

            <div className="form-actions">
              <button type="submit" className="primary-button" disabled={isLoading}>
                Search
              </button>
            </div>
          </form>
        </section>

        <section className="panel" id="staffs">
          <div className="panel-header">
            <div>
              <h2>Staff List</h2>
              <p>{staffs.length} record{staffs.length === 1 ? '' : 's'} loaded.</p>
            </div>

            <div className="report-actions" id="reports">
              <button type="button" className="secondary-button" onClick={() => exportReport('excel')}>
                Export Excel
              </button>
              <button type="button" className="secondary-button" onClick={() => exportReport('pdf')}>
                Export PDF
              </button>
            </div>
          </div>

          {message && <div className="success-alert">{message}</div>}
          {error && <div className="error-alert">{error}</div>}

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Staff ID</th>
                  <th>Full name</th>
                  <th>Birthday</th>
                  <th>Gender</th>
                  <th aria-label="Actions"></th>
                </tr>
              </thead>
              <tbody>
                {staffs.map((staff) => (
                  <tr key={staff.staffId}>
                    <td className="staff-id">{staff.staffId}</td>
                    <td>{staff.fullName}</td>
                    <td>{formatDate(staff.birthday)}</td>
                    <td>
                      <span className={`gender-chip gender-${staff.gender}`}>
                        {getGenderName(staff.gender)}
                      </span>
                    </td>
                    <td>
                      <div className="row-actions">
                        <button type="button" className="ghost-button" onClick={() => openEditForm(staff)}>
                          Edit
                        </button>
                        <button type="button" className="danger-button" onClick={() => deleteStaff(staff.staffId)}>
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}

                {staffs.length === 0 && (
                  <tr>
                    <td colSpan="5" className="empty-state">
                      No staff records found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      </section>

      {isFormOpen && (
        <div className="dialog-backdrop" role="presentation">
          <section className="staff-dialog" role="dialog" aria-modal="true" aria-labelledby="staff-form-title">
            <div className="dialog-header">
              <div>
                <h2 id="staff-form-title">{isEditing ? 'Edit Staff' : 'Add Staff'}</h2>
                <p>{isEditing ? editingStaffId : 'Staff ID will be generated automatically.'}</p>
              </div>
              <button type="button" className="icon-button" onClick={closeForm} aria-label="Close form">
                X
              </button>
            </div>

            <form className="staff-form" onSubmit={saveStaff}>
              <label>
                Full name
                <input
                  value={form.fullName}
                  onChange={(event) => setFormField('fullName', event.target.value)}
                  maxLength="100"
                  placeholder="Enter full name"
                  autoFocus
                />
              </label>

              <label>
                Birthday
                <input
                  type="date"
                  value={form.birthday}
                  onChange={(event) => setFormField('birthday', event.target.value)}
                />
              </label>

              <label>
                Gender
                <select
                  value={form.gender}
                  onChange={(event) => setFormField('gender', event.target.value)}
                >
                  <option value="1">Male</option>
                  <option value="2">Female</option>
                </select>
              </label>

              <div className="dialog-actions">
                <button type="button" className="ghost-button" onClick={closeForm}>
                  Cancel
                </button>
                <button type="submit" className="primary-button" disabled={isLoading}>
                  {isEditing ? 'Save Changes' : 'Create Staff'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </main>
  )

  function setSearchField(field, value) {
    setSearch((current) => ({ ...current, [field]: value }))
  }

  function setFormField(field, value) {
    setForm((current) => ({ ...current, [field]: value }))
  }
}

function SummaryStat({ label, value }) {
  return (
    <article className="summary-stat">
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  )
}

async function apiGet(url) {
  const response = await fetch(url)

  return readApiResponse(response)
}

async function apiSend(url, method, body) {
  const response = await fetch(url, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })

  return readApiResponse(response)
}

async function readApiResponse(response) {
  if (!response.ok) {
    const message = await getFriendlyErrorMessage(response)
    throw new Error(message)
  }

  if (response.status === 204) {
    return null
  }

  return response.json()
}

async function getFriendlyErrorMessage(response) {
  const fallbackMessage = getFallbackMessage(response.status)
  const contentType = response.headers.get('content-type') || ''
  const responseText = await response.text()

  if (!responseText) {
    return fallbackMessage
  }

  if (!contentType.includes('json')) {
    return cleanupMessage(responseText, fallbackMessage)
  }

  try {
    const errorBody = JSON.parse(responseText)

    return parseErrorBody(errorBody, fallbackMessage)
  } catch {
    return cleanupMessage(responseText, fallbackMessage)
  }
}

function parseErrorBody(errorBody, fallbackMessage) {
  if (typeof errorBody === 'string') {
    return cleanupMessage(errorBody, fallbackMessage)
  }

  if (errorBody?.errors) {
    const validationMessages = Object.values(errorBody.errors)
      .flat()
      .filter(Boolean)

    if (validationMessages.length > 0) {
      return validationMessages.join(' ')
    }
  }

  if (errorBody?.message) {
    return cleanupMessage(errorBody.message, fallbackMessage)
  }

  if (errorBody?.title) {
    return cleanupMessage(errorBody.title, fallbackMessage)
  }

  return fallbackMessage
}

function cleanupMessage(message, fallbackMessage) {
  const text = String(message).trim()

  if (!text) {
    return fallbackMessage
  }

  if (text.includes('Microsoft.') || text.includes('System.') || text.includes(' at ')) {
    return 'Something went wrong while processing your request. Please try again.'
  }

  return text.replace(/^"|"$/g, '')
}

function getFallbackMessage(status) {
  if (status === 400) {
    return 'Please check the information and try again.'
  }

  if (status === 404) {
    return 'The staff record could not be found.'
  }

  if (status === 409) {
    return 'This staff record already exists.'
  }

  if (status >= 500) {
    return 'The server could not complete the request. Please try again later.'
  }

  return 'Request failed. Please try again.'
}

function toQueryString(values) {
  const params = new URLSearchParams()

  Object.entries(values).forEach(([key, value]) => {
    if (value !== '') {
      params.append(key, value)
    }
  })

  return params.toString()
}

function validateForm(form) {
  if (!form.fullName.trim()) {
    return 'Full name is required.'
  }

  if (form.fullName.trim().length > 100) {
    return 'Full name must be 100 characters or fewer.'
  }

  if (!form.birthday) {
    return 'Birthday is required.'
  }

  if (!['1', '2'].includes(form.gender)) {
    return 'Gender must be Male or Female.'
  }

  return ''
}

function formatDate(value) {
  return new Intl.DateTimeFormat('en', {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
  }).format(new Date(`${value}T00:00:00`))
}

function getGenderName(gender) {
  return gender === 1 ? 'Male' : 'Female'
}

export default App
