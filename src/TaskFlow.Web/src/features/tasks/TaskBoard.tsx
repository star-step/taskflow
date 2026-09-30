import { useState } from 'react';
import {
  DndContext, DragOverlay, KeyboardSensor, PointerSensor, pointerWithin, rectIntersection, useDraggable, useDroppable,
  useSensor, useSensors, type Announcements, type CollisionDetection, type DragEndEvent, type KeyboardCoordinateGetter,
} from '@dnd-kit/core';
import { STATUSES, STATUS_LABEL, type Task, type TaskStatus } from '../../api/types';
import { Avatar, PriorityBadge, StatusIcon } from '../../components/Badges';
import { statusTextClass } from '../../components/badgeClasses';
import { GripIcon } from '../../components/Icons';
import { StatusSelect } from '../../components/StatusSelect';
import { MOBILE, useMediaQuery } from '../../hooks/useMediaQuery';
import styles from './tasks.module.css';

const stripe = { Critical: 'var(--crit)', High: 'var(--high)', Medium: 'var(--med)', Low: 'var(--low)' } as const;

/** Arrow keys move a lifted card between columns rather than by a few pixels. */
const columnCoordinates: KeyboardCoordinateGetter = (event, { context: { droppableRects, collisionRect } }) => {
  if (!collisionRect || (event.code !== 'ArrowRight' && event.code !== 'ArrowLeft')) return undefined;
  event.preventDefault();
  const columns = STATUSES.map((s) => droppableRects.get(s)).filter((r): r is NonNullable<typeof r> => !!r);
  const centre = collisionRect.left + collisionRect.width / 2;
  const current = columns.findIndex((r) => centre >= r.left && centre <= r.left + r.width);
  const target = columns[Math.min(columns.length - 1, Math.max(0, current + (event.code === 'ArrowRight' ? 1 : -1)))];
  return target ? { x: target.left + 12, y: target.top + 48 } : undefined;
};

/** The column under the pointer wins; keyboard drags (no pointer) fall back to the card's rectangle. */
const columnUnderPointer: CollisionDetection = (args) => {
  const hits = pointerWithin(args);
  return hits.length > 0 ? hits : rectIntersection(args);
};

function title(tasks: Task[], id: string | number) {
  const t = tasks.find((x) => x.id === Number(id));
  return t ? `task #${t.id}, ${t.title}` : `task ${id}`;
}

export function TaskBoard({ tasks, onOpen, onStatus }: { tasks: Task[]; onOpen(t: Task): void; onStatus(t: Task, s: TaskStatus): void }) {
  const isMobile = useMediaQuery(MOBILE);
  const [activeId, setActiveId] = useState<number | null>(null);
  const [mobileColumn, setMobileColumn] = useState<TaskStatus>('ToDo');
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: columnCoordinates }),
  );

  const byStatus = (s: TaskStatus) => tasks.filter((t) => t.status === s);
  const active = tasks.find((t) => t.id === activeId) ?? null;

  const announcements: Announcements = {
    onDragStart: ({ active: a }) => `Picked up ${title(tasks, a.id)}. Use left and right arrows to change column, space to drop.`,
    onDragOver: ({ active: a, over }) => over ? `${title(tasks, a.id)} is over ${STATUS_LABEL[over.id as TaskStatus]}.` : `${title(tasks, a.id)} is not over a column.`,
    onDragEnd: ({ active: a, over }) => over ? `Moved ${title(tasks, a.id)} to ${STATUS_LABEL[over.id as TaskStatus]}.` : `Dropped ${title(tasks, a.id)}.`,
    onDragCancel: ({ active: a }) => `Cancelled. ${title(tasks, a.id)} was not moved.`,
  };

  function onDragEnd({ active: a, over }: DragEndEvent) {
    setActiveId(null);
    const task = tasks.find((t) => t.id === Number(a.id));
    const status = over?.id as TaskStatus | undefined;
    if (task && status && status !== task.status) onStatus(task, status);
  }

  return (
    <>
      <div className={styles.boardTabs}>
        <div className={styles.seg} role="group" aria-label="Column">
          {STATUSES.map((s) => (
            <button key={s} type="button" aria-pressed={mobileColumn === s} onClick={() => setMobileColumn(s)}>
              {STATUS_LABEL[s]} <span className={styles.count}>{byStatus(s).length}</span>
            </button>
          ))}
        </div>
      </div>
      <DndContext sensors={sensors} collisionDetection={columnUnderPointer} accessibility={{ announcements }}
        onDragStart={({ active: a }) => setActiveId(Number(a.id))} onDragEnd={onDragEnd} onDragCancel={() => setActiveId(null)}>
        <div className={styles.board}>
          {STATUSES.map((s) => (
            <Column key={s} status={s} tasks={byStatus(s)} hidden={isMobile && mobileColumn !== s} dragDisabled={isMobile}
              activeId={activeId} onOpen={onOpen} onStatus={onStatus} />
          ))}
        </div>
        <DragOverlay>
          {active && <CardBody task={active} dragging />}
        </DragOverlay>
      </DndContext>
    </>
  );
}

function Column({ status, tasks, hidden, dragDisabled, activeId, onOpen, onStatus }: {
  status: TaskStatus; tasks: Task[]; hidden: boolean; dragDisabled: boolean; activeId: number | null;
  onOpen(t: Task): void; onStatus(t: Task, s: TaskStatus): void;
}) {
  const { setNodeRef, isOver } = useDroppable({ id: status });
  const dragging = activeId !== null && !tasks.some((t) => t.id === activeId);
  return (
    <section ref={setNodeRef} className={`${styles.col} ${isOver && dragging ? styles.isOver : ''}`} hidden={hidden} aria-label={STATUS_LABEL[status]}>
      <h2 className={`${styles.colHead} ${statusTextClass[status]}`}>
        <StatusIcon status={status} />{STATUS_LABEL[status]}<span className={styles.colCount}>{tasks.length}</span>
      </h2>
      {tasks.map((t) => <DraggableCard key={t.id} task={t} disabled={dragDisabled} onOpen={onOpen} onStatus={onStatus} />)}
      {isOver && dragging && <div className={styles.dropSlot}>Drop to move to {STATUS_LABEL[status]}</div>}
      {tasks.length === 0 && !(isOver && dragging) && <p className={styles.colEmpty}>No tasks</p>}
    </section>
  );
}

function DraggableCard({ task, disabled, onOpen, onStatus }: { task: Task; disabled: boolean; onOpen(t: Task): void; onStatus(t: Task, s: TaskStatus): void }) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: task.id, disabled });
  return (
    <div ref={setNodeRef} className={isDragging ? styles.isGhost : undefined}>
      <CardBody task={task} onOpen={onOpen} onStatus={onStatus} handleProps={disabled ? undefined : { ...attributes, ...listeners }} />
    </div>
  );
}

function CardBody({ task, dragging, onOpen, onStatus, handleProps }: {
  task: Task; dragging?: boolean; onOpen?(t: Task): void; onStatus?(t: Task, s: TaskStatus): void;
  handleProps?: Record<string, unknown>;
}) {
  return (
    <article className={`${styles.bcard} ${dragging ? styles.isDragging : ''}`} style={{ ['--stripe' as string]: stripe[task.priority] }}>
      {handleProps && (
        <button type="button" className={styles.grip} aria-label={`Move task #${task.id}`} {...handleProps}><GripIcon /></button>
      )}
      <button type="button" className={`${styles.titleBtn} ${styles.bcardTitle}`} onClick={() => onOpen?.(task)} tabIndex={dragging ? -1 : 0}>
        {task.title}
      </button>
      {onStatus && <span className={styles.bcardStatus}><StatusSelect taskId={task.id} value={task.status} onChange={(s) => onStatus(task, s)} size="lg" /></span>}
      <div className={styles.bcardFoot}>
        <PriorityBadge priority={task.priority} />
        {task.assignedTo && <span title={task.assignedTo}><Avatar name={task.assignedTo} /></span>}
      </div>
    </article>
  );
}
